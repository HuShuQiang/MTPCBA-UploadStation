#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 美团PCBA过站上传工具
{
    /// <summary>串口通讯参数。修改后需重新连接才会生效。</summary>
    public sealed class SerialCommOptions
    {
        /// <summary>端口名，如 COM3。</summary>
        public string PortName { get; set; } = "COM1";

        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>收发编码。默认 ASCII，务必按设备协议改成 UTF8 / GB2312 等。</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>行结束符，ReadLine / SendLine 使用。</summary>
        public string NewLine { get; set; } = "\n";

        /// <summary>DTR 电平。部分 USB 转串口芯片在 DTR 拉低时会复位目标板。</summary>
        public bool DtrEnable { get; set; } = true;

        /// <summary>RTS 电平。</summary>
        public bool RtsEnable { get; set; } = true;

        /// <summary>接收缓冲区大小，默认 4096，高频数据建议调大。</summary>
        public int ReadBufferSize { get; set; } = 8192;

        /// <summary>发送缓冲区大小，默认 2048。</summary>
        public int WriteBufferSize { get; set; } = 4096;

        /// <summary>读超时（毫秒）。默认无限等待会导致线程永久卡死，必须设置。</summary>
        public int ReadTimeout { get; set; } = 200;

        /// <summary>写超时（毫秒）。</summary>
        public int WriteTimeout { get; set; } = 1000;

        /// <summary>掉线后自动重连的间隔（毫秒）。</summary>
        public int ReconnectIntervalMs { get; set; } = 2000;

        /// <summary>切出帧时是否额外抛出文本帧事件。</summary>
        public bool RaiseTextFrame { get; set; } = true;
    }

    /// <summary>通讯统计。</summary>
    public readonly struct SerialCommStats
    {
        public SerialCommStats(long bytesSent, long bytesReceived, long framesSent, long framesReceived)
        {
            BytesSent = bytesSent;
            BytesReceived = bytesReceived;
            FramesSent = framesSent;
            FramesReceived = framesReceived;
        }

        public long BytesSent { get; }
        public long BytesReceived { get; }
        public long FramesSent { get; }
        public long FramesReceived { get; }
    }

    /// <summary>断线事件参数。</summary>
    public sealed class SerialDisconnectedEventArgs : EventArgs
    {
        public SerialDisconnectedEventArgs(Exception? reason)
        {
            Reason = reason;
        }

        /// <summary>断线原因，可能为 null（主动关闭）。</summary>
        public Exception? Reason { get; }
    }

    /// <summary>
    /// 独立的串口通讯类：连接管理、后台读线程、拆包、断线重连、UI 线程回调。
    ///
    /// 线程模型：
    ///   - 读线程：后台线程，通过 ReadTimeout 轮询退出，不阻塞 UI。
    ///   - 事件回调：默认投递到构造时捕获的 SynchronizationContext（WinForms / WPF 主线程）。
    ///
    /// 使用示例：
    ///   var comm = new SerialComm("COM3", 115200)
    ///   {
    ///       FrameParser = FrameParsers.Delimited("\r\n")
    ///   };
    ///   comm.FrameReceived += (s, frame) => { /* 已在 UI 线程 */ };
    ///   comm.Start();   // 打开并自动重连
    /// </summary>
    public sealed class SerialComm : IDisposable
    {
        private readonly SerialCommOptions _opt;
        private readonly FrameBuffer _frameBuffer = new();
        private readonly object _sendLock = new();
        private readonly object _stateLock = new();

        private SerialPort? _port;
        private Thread? _reader;
        private volatile bool _running;
        private CancellationTokenSource? _cts;
        private Task? _supervisor;
        private DateTime _nextAttemptUtc = DateTime.MinValue;

        private long _bytesSent;
        private long _bytesReceived;
        private long _framesSent;
        private long _framesReceived;

        public SerialComm(SerialCommOptions options)
        {
            _opt = options ?? throw new ArgumentNullException(nameof(options));
            Dispatcher = SynchronizationContext.Current;
        }

        public SerialComm(string portName, int baudRate = 115200)
            : this(new SerialCommOptions { PortName = portName, BaudRate = baudRate })
        {
        }

        /// <summary>通讯参数。</summary>
        public SerialCommOptions Options => _opt;

        /// <summary>
        /// 事件回调调度器。默认取构造时的 SynchronizationContext
        /// （WinForms 主线程 / WPF 主线程）。设为 null 则在触发线程上同步回调。
        /// </summary>
        public SynchronizationContext? Dispatcher { get; set; }

        /// <summary>
        /// 拆包器：从接收缓冲区中切出一帧。返回 (起始偏移, 长度)；数据不足时返回 null。
        /// 解析器可以就地修改缓冲区以丢弃垃圾字节（框架内置的解析器都支持重同步）。
        /// 为 null 时不拆包，只通过 RawDataReceived 抛原始数据。
        /// </summary>
        public Func<List<byte>, (int Offset, int Length)?>? FrameParser
        {
            get => _frameBuffer.Parser;
            set => _frameBuffer.Parser = value;
        }

        /// <summary>端口名。赋值会断开当前连接（若在自动重连模式，会用新端口重连）。</summary>
        public string PortName
        {
            get => _opt.PortName;
            set
            {
                if (string.Equals(_opt.PortName, value, StringComparison.OrdinalIgnoreCase)) return;
                _opt.PortName = value;
                if (IsOpen) Close();
            }
        }

        public bool IsOpen
        {
            get
            {
                lock (_stateLock)
                {
                    return _port is { IsOpen: true };
                }
            }
        }

        public SerialCommStats Stats => new(
            Interlocked.Read(ref _bytesSent),
            Interlocked.Read(ref _bytesReceived),
            Interlocked.Read(ref _framesSent),
            Interlocked.Read(ref _framesReceived));

        /// <summary>切好的一帧数据。</summary>
        public event EventHandler<byte[]>? FrameReceived;

        /// <summary>帧的文本形式（仅当帧内容可解码为文本时触发）。</summary>
        public event EventHandler<string>? TextFrameReceived;

        /// <summary>
        /// 原始数据块（未拆包）。
        /// ★ 触发粒度 = 一次 SerialPort.Read() 调用。Read() 返回的是"此刻驱动缓冲区里已有的字节"，
        ///   设备按位到达时一次往往只读到 1 个字节，所以这个事件**不能**当作"一行/一帧"来用。
        ///   只想按帧/按行拿数据请用 FrameReceived / TextFrameReceived。
        /// </summary>
        public event EventHandler<byte[]>? RawDataReceived;

        /// <summary>运行日志。</summary>
        public event EventHandler<string>? Log;

        /// <summary>通讯异常。</summary>
        public event EventHandler<Exception>? Error;

        /// <summary>连接成功。</summary>
        public event EventHandler? Connected;

        /// <summary>连接断开。</summary>
        public event EventHandler<SerialDisconnectedEventArgs>? Disconnected;

        #region 连接管理

        /// <summary>打开串口并启动读线程。失败时抛出异常（端口占用 / 设备不存在）。</summary>
        public void Open()
        {
            lock (_stateLock)
            {
                if (_port is { IsOpen: true }) return;

                var port = new SerialPort(
                    _opt.PortName,
                    _opt.BaudRate,
                    _opt.Parity,
                    _opt.DataBits,
                    _opt.StopBits)
                {
                    Encoding = _opt.Encoding,
                    NewLine = _opt.NewLine,
                    ReadTimeout = _opt.ReadTimeout,
                    WriteTimeout = _opt.WriteTimeout,
                    ReadBufferSize = _opt.ReadBufferSize,
                    WriteBufferSize = _opt.WriteBufferSize,
                    DtrEnable = _opt.DtrEnable,
                    RtsEnable = _opt.RtsEnable,
                };

                port.Open();
                try
                {
                    port.DiscardInBuffer();
                }
                catch
                {
                    // 忽略：部分虚拟口不支持
                }

                _port = port;
                _running = true;

                _reader = new Thread(ReadLoop)
                {
                    IsBackground = true,
                    Name = "SerialComm-" + _opt.PortName,
                };
                _reader.Start();
            }

            WriteLog(string.Format(
                "已连接 {0} @ {1} {2}{3}{4}",
                _opt.PortName,
                _opt.BaudRate,
                _opt.DataBits,
                ParityText(_opt.Parity),
                _opt.StopBits));
            Raise(Connected);
        }

        /// <summary>关闭串口并停止读线程。</summary>
        public void Close()
        {
            SerialPort? port;
            lock (_stateLock)
            {
                _running = false;
                port = _port;
                _port = null;
            }

            try
            {
                if (port is { IsOpen: true }) port.Close();
            }
            catch
            {
                // 忽略关闭异常
            }

            port?.Dispose();
            ClearReceiveBuffer();
            WriteLog("已断开");
        }

        /// <summary>启动带自动重连的后台监督线程。</summary>
        public void Start()
        {
            if (_cts is not null) return;
            _cts = new CancellationTokenSource();
            _supervisor = Task.Run(() => SuperviseAsync(_cts.Token));
        }

        /// <summary>停止自动重连并断开。</summary>
        public void Stop()
        {
            var cts = _cts;
            _cts = null;
            try
            {
                cts?.Cancel();
            }
            catch
            {
                // 忽略
            }

            try
            {
                _supervisor?.Wait(TimeSpan.FromSeconds(3));
            }
            catch
            {
                // 忽略
            }

            _supervisor = null;
            Close();
            cts?.Dispose();
        }

        private async Task SuperviseAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!IsOpen)
                    {
                        if (DateTime.UtcNow >= _nextAttemptUtc)
                        {
                            try
                            {
                                Open();
                                _nextAttemptUtc = DateTime.MinValue;
                            }
                            catch (Exception ex)
                            {
                                WriteLog(string.Format(
                                    "连接失败: {0}（{1}ms 后重试）",
                                    ex.Message,
                                    _opt.ReconnectIntervalMs));
                                _nextAttemptUtc = DateTime.UtcNow.AddMilliseconds(_opt.ReconnectIntervalMs);
                            }
                        }
                        else
                        {
                            await Task.Delay(200, ct).ConfigureAwait(false);
                            continue;
                        }
                    }

                    await Task.Delay(500, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    RaiseError(ex);
                    try
                    {
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        #region 发送

        /// <summary>发送字节块。</summary>
        public void Send(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var port = RequireOpen();
            lock (_sendLock)
            {
                port.Write(data, 0, data.Length);
                Interlocked.Add(ref _bytesSent, data.Length);
                Interlocked.Increment(ref _framesSent);
            }
        }

        /// <summary>发送字节块的一部分。</summary>
        public void Send(byte[] data, int offset, int count)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var port = RequireOpen();
            lock (_sendLock)
            {
                port.Write(data, offset, count);
                Interlocked.Add(ref _bytesSent, count);
                Interlocked.Increment(ref _framesSent);
            }
        }

        /// <summary>按编码发送文本。</summary>
        public void Send(string text) => Send(_opt.Encoding.GetBytes(text));

        /// <summary>发送一行（自动追加 NewLine）。</summary>
        public void SendLine(string text) => Send(text + _opt.NewLine);

        /// <summary>发送十六进制字符串，支持空格 / 冒号 / 短横线分隔。</summary>
        public void SendHex(string hex) => Send(Checksum.ToBytes(hex));

        private SerialPort RequireOpen()
        {
            var port = _port;
            if (port is null || !port.IsOpen)
            {
                throw new InvalidOperationException("串口未打开");
            }

            return port;
        }

        #endregion

        #region 接收

        private void ReadLoop()
        {
            var port = _port;
            if (port is null) return;

            var buf = new byte[4096];
            while (_running)
            {
                int n;
                try
                {
                    n = port.Read(buf, 0, buf.Length);
                }
                catch (TimeoutException)
                {
                    continue;
                }
                catch (IOException ex)
                {
                    HandleDisconnected(ex);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    HandleDisconnected(ex);
                    break;
                }
                catch (Exception ex)
                {
                    RaiseError(ex);
                    break;
                }

                if (n <= 0) continue;

                Interlocked.Add(ref _bytesReceived, n);

                var chunk = new byte[n];
                Buffer.BlockCopy(buf, 0, chunk, 0, n);
                Raise(RawDataReceived, chunk);

                // 累积进缓冲区，然后把当前凑齐的所有完整帧都切出来。
                // 注意：SerialPort.Read() 返回的是"此刻缓冲区里已有的字节"，
                // 设备按位到达时一次往往只读到 1 个字节，所以这里必须依赖累积缓冲，
                // 而不是假设一次 Read 就是一帧。
                _frameBuffer.Append(chunk);
                var frames = _frameBuffer.Drain();

                foreach (var frame in frames)
                {
                    Interlocked.Increment(ref _framesReceived);
                    Raise(FrameReceived, frame);

                    if (_opt.RaiseTextFrame)
                    {
                        var text = DecodeText(frame);
                        if (text is not null) Raise(TextFrameReceived, text);
                    }
                }
            }
        }

        private void HandleDisconnected(Exception reason)
        {
            _running = false;
            _nextAttemptUtc = DateTime.UtcNow.AddMilliseconds(_opt.ReconnectIntervalMs);
            try
            {
                _port?.Close();
            }
            catch
            {
                // 忽略
            }

            WriteLog("连接断开: " + reason.Message);
            Raise(Disconnected, new SerialDisconnectedEventArgs(reason));
        }

        /// <summary>
        /// 尝试把一帧解码成可读文本。
        /// 含 \r \n \t 之外的二进制控制字符时判定为二进制数据，返回 false。
        /// </summary>
        public static bool TryDecodeText(byte[] frame, Encoding encoding, out string text)
        {
            text = string.Empty;
            if (frame is null || frame.Length == 0) return false;

            try
            {
                text = encoding.GetString(frame);
            }
            catch
            {
                return false;
            }

            foreach (var c in text)
            {
                if (c < 0x20 && c != '\r' && c != '\n' && c != '\t') return false;
            }

            text = text.TrimEnd('\r', '\n');
            return true;
        }

        private string? DecodeText(byte[] frame)
        {
            return TryDecodeText(frame, _opt.Encoding, out var text) ? text : null;
        }

        /// <summary>清空接收缓冲区。</summary>
        public void ClearReceiveBuffer()
        {
            try
            {
                _port?.DiscardInBuffer();
            }
            catch
            {
                // 忽略
            }

            _frameBuffer.Clear();
        }

        #endregion

        #region 事件与日志

        private void Raise(EventHandler? handler)
        {
            if (handler is null) return;
            Post(() => handler(this, EventArgs.Empty));
        }

        private void Raise<T>(EventHandler<T>? handler, T arg)
        {
            if (handler is null) return;
            Post(() => handler(this, arg));
        }

        private void Post(Action action)
        {
            var ctx = Dispatcher;
            if (ctx is null)
            {
                action();
                return;
            }

            try
            {
                ctx.Post(_ => action(), null);
            }
            catch
            {
                try
                {
                    action();
                }
                catch
                {
                    // 忽略
                }
            }
        }

        private void WriteLog(string message)
        {
            var handler = Log;
            if (handler is null) return;
            var line = string.Format("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, message);
            Post(() => handler(this, line));
        }

        private void RaiseError(Exception ex) => Raise(Error, ex);

        private static string ParityText(Parity parity) => parity switch
        {
            Parity.None => "N",
            Parity.Odd => "O",
            Parity.Even => "E",
            _ => "?",
        };

        #endregion

        public void Dispose()
        {
            Stop();
            FrameReceived = null;
            TextFrameReceived = null;
            RawDataReceived = null;
            Log = null;
            Error = null;
            Connected = null;
            Disconnected = null;
        }
    }
}

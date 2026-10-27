#nullable enable
using System;
using System.Collections.Generic;

namespace 美团PCBA过站上传工具
{
    /// <summary>
    /// 接收缓冲区 + 拆包器。
    ///
    /// 独立成类是为了能脱离真实串口做单元测试（逐字节喂、一次喂多帧等）。
    ///
    /// 关键点：
    ///   - 缓冲区是**累积**的，解析器在累积后的数据上工作，所以粘包/半包都能正确还原。
    ///   - Drain() 会一次把缓冲区里**所有**完整帧都切出来，避免"剩下的帧滞留到下次才有数据才处理"。
    /// </summary>
    public sealed class FrameBuffer
    {
        private readonly List<byte> _buffer = new();
        private readonly object _sync = new();

        /// <summary>
        /// 拆包器：从缓冲区切出一帧，返回 (起始偏移, 长度)；数据不足返回 null。
        /// 解析器可以就地修改缓冲区以丢弃垃圾字节。
        /// 为 null 时不拆包。
        /// </summary>
        public Func<List<byte>, (int Offset, int Length)?>? Parser { get; set; }

        /// <summary>当前缓冲区里尚未成帧的字节数。</summary>
        public int BufferedCount
        {
            get
            {
                lock (_sync)
                {
                    return _buffer.Count;
                }
            }
        }

        /// <summary>追加数据到缓冲区。</summary>
        public void Append(byte[] data, int offset, int count)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            lock (_sync)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    _buffer.Add(data[i]);
                }
            }
        }

        /// <summary>追加数据到缓冲区。</summary>
        public void Append(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            Append(data, 0, data.Length);
        }

        /// <summary>
        /// 切出当前缓冲区里所有已经完整的帧。返回空列表表示还没凑齐一帧。
        /// </summary>
        public List<byte[]> Drain()
        {
            var frames = new List<byte[]>();
            lock (_sync)
            {
                var parser = Parser;
                if (parser is null) return frames;

                while (true)
                {
                    var parsed = parser(_buffer);
                    if (parsed is not { } p) break;
                    if (p.Length <= 0 || p.Offset < 0 || p.Offset + p.Length > _buffer.Count) break;

                    frames.Add(_buffer.GetRange(p.Offset, p.Length).ToArray());
                    _buffer.RemoveRange(0, p.Offset + p.Length);
                }
            }

            return frames;
        }

        /// <summary>清空缓冲区。</summary>
        public void Clear()
        {
            lock (_sync)
            {
                _buffer.Clear();
            }
        }
    }
}

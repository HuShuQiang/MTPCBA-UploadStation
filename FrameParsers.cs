#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace 美团PCBA过站上传工具
{
    /// <summary>常用校验算法。</summary>
    public static class Checksum
    {
        /// <summary>CRC16/MODBUS（多项式 0xA001，初值 0xFFFF）。校验值："123456789" -> 0x4B37。</summary>
        public static ushort Crc16Modbus(byte[] data, int offset = 0, int count = -1)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (count < 0) count = data.Length - offset;

            ushort crc = 0xFFFF;
            for (int i = offset; i < offset + count; i++)
            {
                crc ^= data[i];
                for (int b = 0; b < 8; b++)
                {
                    crc = (crc & 1) != 0
                        ? (ushort)((crc >> 1) ^ 0xA001)
                        : (ushort)(crc >> 1);
                }
            }

            return crc;
        }

        /// <summary>CRC16/XMODEM（CCITT-FALSE，多项式 0x1021，初值 0x0000）。校验值："123456789" -> 0x31C3。</summary>
        public static ushort Crc16XModem(byte[] data, int offset = 0, int count = -1)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (count < 0) count = data.Length - offset;

            ushort crc = 0x0000;
            for (int i = offset; i < offset + count; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int b = 0; b < 8; b++)
                {
                    crc = (crc & 0x8000) != 0
                        ? (ushort)((crc << 1) ^ 0x1021)
                        : (ushort)(crc << 1);
                }
            }

            return crc;
        }

        /// <summary>
        /// CRC8/MAXIM（Dallas 1-Wire，多项式 0x31 的反射形式 0x8C，初值 0x00，LSB 优先）。
        /// 校验值："123456789" -> 0xA1。
        /// </summary>
        public static byte Crc8(byte[] data, int offset = 0, int count = -1)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (count < 0) count = data.Length - offset;

            byte crc = 0x00;
            for (int i = offset; i < offset + count; i++)
            {
                crc ^= data[i];
                for (int b = 0; b < 8; b++)
                {
                    crc = (crc & 0x01) != 0
                        ? (byte)((crc >> 1) ^ 0x8C)
                        : (byte)(crc >> 1);
                }
            }

            return crc;
        }

        /// <summary>累加和，取低 8 位。</summary>
        public static byte Sum8(byte[] data, int offset = 0, int count = -1)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (count < 0) count = data.Length - offset;

            int sum = 0;
            for (int i = offset; i < offset + count; i++) sum += data[i];
            return (byte)(sum & 0xFF);
        }

        /// <summary>十六进制字符串转字节数组。支持空格 / 冒号 / 短横线分隔。</summary>
        public static byte[] ToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Array.Empty<byte>();

            var sb = new StringBuilder(hex.Length);
            foreach (var c in hex)
            {
                if (!char.IsWhiteSpace(c) && c != '-' && c != ':') sb.Append(c);
            }

            if (sb.Length % 2 != 0)
            {
                throw new FormatException("十六进制字符串长度必须是偶数");
            }

            var result = new byte[sb.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Convert.ToByte(sb.ToString(i * 2, 2), 16);
            }

            return result;
        }

        /// <summary>字节数组转十六进制字符串（空格分隔）。</summary>
        public static string ToHex(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            return BitConverter.ToString(data).Replace("-", " ");
        }
    }

    /// <summary>
    /// 内置拆包器。全部返回 (起始偏移, 长度)，数据不足返回 null。
    /// 所有解析器都支持"重同步"：遇到非法数据会自行丢弃并继续在后续字节中寻找合法帧头。
    /// </summary>
    public static class FrameParsers
    {
        /// <summary>固定长度帧。</summary>
        public static Func<List<byte>, (int Offset, int Length)?> FixedLength(int frameLength)
        {
            if (frameLength <= 0) throw new ArgumentOutOfRangeException(nameof(frameLength));
            return buf => buf.Count < frameLength ? null : (0, frameLength);
        }

        /// <summary>以分隔符结尾的帧（帧包含分隔符本身）。</summary>
        public static Func<List<byte>, (int Offset, int Length)?> Delimited(string delimiter)
            => Delimited(Encoding.UTF8.GetBytes(delimiter));

        /// <summary>以分隔符结尾的帧（帧包含分隔符本身）。</summary>
        public static Func<List<byte>, (int Offset, int Length)?> Delimited(byte delimiter)
            => Delimited(new[] { delimiter });

        /// <summary>以分隔符结尾的帧（帧包含分隔符本身）。</summary>
        public static Func<List<byte>, (int Offset, int Length)?> Delimited(byte[] delimiter)
        {
            if (delimiter is null) throw new ArgumentNullException(nameof(delimiter));
            if (delimiter.Length == 0) throw new ArgumentException("分隔符不能为空", nameof(delimiter));

            return buf =>
            {
                int idx = IndexOf(buf, delimiter, 0);
                if (idx < 0) return null;
                return (0, idx + delimiter.Length);
            };
        }

        /// <summary>
        /// 通用二进制帧：[帧头][长度字段][数据][帧尾]
        /// </summary>
        /// <param name="header">帧头字节；传空数组表示无帧头。</param>
        /// <param name="lengthOffset">长度字段在整帧中的起始下标（0 基，帧头之后）。</param>
        /// <param name="lengthSize">长度字段字节数：1 / 2 / 4。</param>
        /// <param name="bigEndian">长度字段是否大端序。</param>
        /// <param name="lengthCoversLengthField">长度字段的值是否包含长度字段自身。</param>
        /// <param name="trailerSize">帧尾固定字节数（不计入长度字段）。</param>
        /// <param name="validator">整帧校验函数，返回 false 表示该帧无效将被丢弃并重新找帧头。</param>
        public static Func<List<byte>, (int Offset, int Length)?> Binary(
            byte[] header,
            int lengthOffset,
            int lengthSize,
            bool bigEndian = false,
            bool lengthCoversLengthField = false,
            int trailerSize = 0,
            Func<byte[], bool>? validator = null)
        {
            if (header is null) throw new ArgumentNullException(nameof(header));
            if (lengthOffset < 0) throw new ArgumentOutOfRangeException(nameof(lengthOffset));
            if (lengthSize != 1 && lengthSize != 2 && lengthSize != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthSize), "长度字段字节数只能是 1 / 2 / 4");
            }

            return buf =>
            {
                while (true)
                {
                    if (header.Length > 0)
                    {
                        int pos = IndexOf(buf, header, 0);
                        if (pos < 0)
                        {
                            // 保留可能不完整的帧头前缀，其余丢弃
                            int keep = Math.Min(buf.Count, header.Length - 1);
                            if (keep < buf.Count) buf.RemoveRange(0, buf.Count - keep);
                            return null;
                        }

                        if (pos > 0) buf.RemoveRange(0, pos);
                    }

                    if (buf.Count < lengthOffset + lengthSize) return null;

                    int value = ReadInt(buf, lengthOffset, lengthSize, bigEndian);
                    int dataLength = lengthCoversLengthField ? value - lengthSize : value;
                    if (dataLength < 0)
                    {
                        buf.RemoveAt(0);
                        continue;
                    }

                    int total = lengthOffset + lengthSize + dataLength + trailerSize;
                    if (buf.Count < total) return null;

                    var frame = buf.GetRange(0, total).ToArray();
                    if (validator is null || validator(frame)) return (0, total);

                    // 校验失败：丢弃 1 字节重新找帧头
                    buf.RemoveAt(0);
                }
            };
        }

        /// <summary>
        /// Modbus RTU 帧：地址(1) + 功能码(1) + 数据 + CRC16(2, 低字节在前)。
        /// </summary>
        public static Func<List<byte>, (int Offset, int Length)?> ModbusRtu()
        {
            return buf =>
            {
                while (buf.Count >= 2)
                {
                    byte func = buf[1];
                    int frameLength;

                    if (func == 0x03 || func == 0x04)
                    {
                        // 读寄存器 / 读输入寄存器：第 3 字节是字节数
                        if (buf.Count < 3) return null;
                        int byteCount = buf[2];
                        frameLength = 3 + byteCount + 2;
                    }
                    else if ((func & 0x80) != 0)
                    {
                        // 异常响应：地址 + 功能码|0x80 + 异常码 + CRC
                        frameLength = 5;
                    }
                    else
                    {
                        // 0x01 / 0x02 / 0x0F / 0x10 等固定长度请求
                        frameLength = 8;
                    }

                    if (buf.Count < frameLength) return null;

                    var frame = buf.GetRange(0, frameLength).ToArray();
                    if (VerifyCrc16Modbus(frame)) return (0, frameLength);

                    buf.RemoveAt(0);
                }

                return null;
            };
        }

        /// <summary>
        /// 校验 Modbus RTU 的 CRC16（低字节在前）。可作为 validator 直接传方法组。
        /// </summary>
        public static bool VerifyCrc16Modbus(byte[] frame) => VerifyCrc16Modbus(frame, 2);

        /// <summary>
        /// 校验 Modbus RTU 的 CRC16（低字节在前），CRC 位于帧尾 crcLengthFromEnd 字节处。
        /// </summary>
        public static bool VerifyCrc16Modbus(byte[] frame, int crcLengthFromEnd)
        {
            if (frame is null) throw new ArgumentNullException(nameof(frame));
            if (frame.Length < crcLengthFromEnd) return false;

            int dataLength = frame.Length - crcLengthFromEnd;
            ushort calculated = Checksum.Crc16Modbus(frame, 0, dataLength);
            ushort received = (ushort)(frame[dataLength] | (frame[dataLength + 1] << 8));
            return calculated == received;
        }

        /// <summary>在缓冲区中查找字节序列。</summary>
        private static int IndexOf(List<byte> buf, byte[] pattern, int start)
        {
            if (pattern.Length == 0) return start;
            int limit = buf.Count - pattern.Length;
            for (int i = Math.Max(0, start); i <= limit; i++)
            {
                bool matched = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buf[i + j] != pattern[j])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return i;
            }

            return -1;
        }

        private static int ReadInt(List<byte> buf, int offset, int size, bool bigEndian)
        {
            if (size == 1) return buf[offset];

            int value = 0;
            if (bigEndian)
            {
                for (int i = 0; i < size; i++) value = (value << 8) | buf[offset + i];
            }
            else
            {
                for (int i = size - 1; i >= 0; i--) value = (value << 8) | buf[offset + i];
            }

            return value;
        }
    }
}

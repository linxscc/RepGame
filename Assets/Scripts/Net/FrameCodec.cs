using System;
using System.IO;

namespace RepGame.Net
{
    /// <summary>
    /// 消息帧编解码：4 字节大端序长度 + 消息体。与服务器 protocol/frame.go 保持一致。
    /// </summary>
    public static class FrameCodec
    {
        public const int HeaderSize = 4;
        public const int MaxFrameSize = 1 << 20; // 1MB

        public static byte[] Encode(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length > MaxFrameSize)
                throw new InvalidDataException($"frame too large ({payload.Length} bytes)");

            var frame = new byte[HeaderSize + payload.Length];
            frame[0] = (byte)(payload.Length >> 24);
            frame[1] = (byte)(payload.Length >> 16);
            frame[2] = (byte)(payload.Length >> 8);
            frame[3] = (byte)payload.Length;
            Buffer.BlockCopy(payload, 0, frame, HeaderSize, payload.Length);
            return frame;
        }

        /// <summary>
        /// 阻塞读取一条完整消息；连接正常关闭时返回 null。
        /// </summary>
        public static byte[] Read(Stream stream)
        {
            var header = new byte[HeaderSize];
            if (!ReadExactly(stream, header, HeaderSize))
                return null;

            int size = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (size < 0 || size > MaxFrameSize)
                throw new InvalidDataException($"frame too large ({size} bytes)");

            var payload = new byte[size];
            if (!ReadExactly(stream, payload, size))
                throw new EndOfStreamException("connection closed in the middle of a frame");
            return payload;
        }

        private static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                {
                    if (offset == 0) return false; // 帧边界上正常关闭
                    throw new EndOfStreamException("connection closed in the middle of a frame");
                }
                offset += read;
            }
            return true;
        }
    }
}

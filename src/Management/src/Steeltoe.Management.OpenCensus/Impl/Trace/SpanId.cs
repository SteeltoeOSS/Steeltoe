using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public sealed class SpanId : ISpanId
    {
        public const int SIZE = 8;
        public static readonly SpanId INVALID = new SpanId(new byte[SIZE]);

        private readonly byte[] bytes;

        public byte[] Bytes
        {
            get
            {
                byte[] copyOf = new byte[SIZE];
                Buffer.BlockCopy(bytes, 0, copyOf, 0, SIZE);
                return copyOf;
            }
        }

        private SpanId(byte[] bytes) 
        {
            this.bytes = bytes;
        }

        public static ISpanId FromBytes(byte[] buffer)
        {
            if( buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length != SIZE)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid size: expected {0}, got {1}", SIZE, buffer.Length));
            }

            byte[] bytesCopied = new byte[SIZE];
            Buffer.BlockCopy(buffer, 0, bytesCopied, 0, SIZE);
            return new SpanId(bytesCopied);
        }

        public static ISpanId FromBytes(byte[] src, int srcOffset)
        {
            byte[] bytes = new byte[SIZE];
            Buffer.BlockCopy(src, srcOffset, bytes, 0, SIZE);
            return new SpanId(bytes);
        }

        public static ISpanId FromLowerBase16(string src)
        {
            if (src.Length != 2 * SIZE)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid size: expected {0}, got {1}", 2 * SIZE, src.Length));
            }
            byte[] bytes = Arrays.StringToByteArray(src);
            return new SpanId(bytes);

        }

        public static ISpanId GenerateRandomId(IRandomGenerator random)
        {
            byte[] bytes = new byte[SIZE];
            do
            {
                random.NextBytes(bytes);
            } while (Arrays.Equals(bytes, INVALID.bytes));
            return new SpanId(bytes);
        }

        public void CopyBytesTo(byte[] dest, int destOffset)
        {
            Buffer.BlockCopy(bytes, 0, dest, destOffset, SIZE);
        }

        public bool IsValid
        {
            get { return !Arrays.Equals(bytes, INVALID.bytes); }
        }

        public string ToLowerBase16()
        {
            var bytes = Bytes;
            return Arrays.ByteArrayToString(bytes);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is SpanId)) {
                return false;
            }

            SpanId that = (SpanId)obj;
            return Arrays.Equals(bytes, that.bytes);
        }


        public override int GetHashCode()
        {
            return Arrays.GetHashCode(bytes);
        }

        public override string ToString()
        {
            return "SpanId{"
                + "bytes=" + ToLowerBase16()
                + "}";

        }


        public int CompareTo(ISpanId other)
        {
            SpanId that = other as SpanId;
            for (int i = 0; i < SIZE; i++)
            {
                if (bytes[i] != that.bytes[i])
                {
                    sbyte b1 = (sbyte)bytes[i];
                    sbyte b2 = (sbyte)that.bytes[i];

                    return b1 < b2 ? -1 : 1;
                }
            }
            return 0;
        }
    }
}

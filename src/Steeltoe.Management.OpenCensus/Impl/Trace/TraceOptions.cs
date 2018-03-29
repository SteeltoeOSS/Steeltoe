using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public sealed class TraceOptions
    {
        // Default options. Nothing set.
        internal const byte DEFAULT_OPTIONS = 0;
        // Bit to represent whether trace is sampled or not.
        internal const byte IS_SAMPLED = 0x1;

        public const int SIZE = 1;

        public static readonly TraceOptions DEFAULT = new TraceOptions(DEFAULT_OPTIONS);

        // The set of enabled features is determined by all the enabled bits.
        private byte options;

        // Creates a new TraceOptions with the given options.
        internal TraceOptions(byte options)
        {
            this.options = options;
        }

        public static TraceOptions FromBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length != SIZE)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid size: expected {0}, got {1}", SIZE, buffer.Length));
            }

            byte[] bytesCopied = new byte[SIZE];
            Buffer.BlockCopy(buffer, 0, bytesCopied, 0, SIZE);
            return new TraceOptions(bytesCopied[0]);
        }

        public static TraceOptions FromBytes(byte[] src, int srcOffset)
        {
            if (srcOffset < 0 || srcOffset >= src.Length)
            {
                throw new IndexOutOfRangeException("srcOffset");
            }

            return new TraceOptions(src[srcOffset]);
        }

        public static TraceOptionsBuilder Builder()
        {
            return new TraceOptionsBuilder(DEFAULT_OPTIONS);
        }

        public static TraceOptionsBuilder Builder(TraceOptions traceOptions)
        {
            return new TraceOptionsBuilder(traceOptions.options);
        }

        public byte[] Bytes
        {
            get
            {
                byte[] bytes = new byte[SIZE];
                bytes[0] = options;
                return bytes;
            }
        }

        public void CopyBytesTo(byte[] dest, int destOffset)
        {
            if (destOffset < 0 || destOffset >= dest.Length)
            {
                throw new IndexOutOfRangeException("destOffset");
            }

            dest[destOffset] = options;
        }

        public bool IsSampled
        {
            get
            {
                return HasOption(IS_SAMPLED);
            }
            //set
            //{
            //    if (value)
            //    {
            //        SetOption(IS_SAMPLED);
            //    } else
            //    {
            //        ClearOption(IS_SAMPLED);
            //    }
            //}
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is TraceOptions)) {
                return false;
            }

            TraceOptions that = (TraceOptions)obj;
            return options == that.options;
        }


        public override int GetHashCode()
        {

            int result = 31 * 1 + options;
            return result;
        }


        public override string ToString()
        {
            return "TraceOptions{"
                + "sampled=" + IsSampled 
                + "}";
        }

        internal sbyte Options
        {
            get { return (sbyte) options; }
        }

        private bool HasOption(int mask)
        {
            return (options & mask) != 0;
        }

        private void ClearOption(int mask)
        {
            options = (byte)(options & ~mask);
        }

        private void SetOption(int mask)
        {
            options = (byte)(options | mask);
        }
    }
}

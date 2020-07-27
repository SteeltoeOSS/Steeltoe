﻿// <copyright file="TraceId.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace
{
    using System;
    using OpenCensus.Utils;

    public sealed class TraceId : ITraceId
    {
        public const int Size = 16;
        private static readonly TraceId InvalidTraceId = new TraceId(new byte[Size]);

        private readonly byte[] bytes;

        private TraceId(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public static ITraceId Invalid
        {
            get
            {
                return InvalidTraceId;
            }
        }

        public byte[] Bytes
        {
            get
            {
                var copyOf = new byte[Size];
                Buffer.BlockCopy(bytes, 0, copyOf, 0, Size);
                return copyOf;
            }
        }

        public bool IsValid
        {
            get
            {
                return !Arrays.Equals(bytes, InvalidTraceId.bytes);
            }
        }

        public long LowerLong
        {
            get
            {
                long result = 0;
                for (var i = 0; i < 8; i++)
                {
                    result <<= 8;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                    result |= bytes[i] & 0xff;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                }

                if (result < 0)
                {
                    return -result;
                }

                return result;
            }
        }

        public static ITraceId FromBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length != Size)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid size: expected {0}, got {1}", Size, buffer.Length));
            }

            var bytesCopied = new byte[Size];
            Buffer.BlockCopy(buffer, 0, bytesCopied, 0, Size);
            return new TraceId(bytesCopied);
        }

        public static ITraceId FromBytes(byte[] src, int srcOffset)
        {
            var bytes = new byte[Size];
            Buffer.BlockCopy(src, srcOffset, bytes, 0, Size);
            return new TraceId(bytes);
        }

        public static ITraceId FromLowerBase16(string src)
        {
            if (src.Length != 2 * Size)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid size: expected {0}, got {1}", 2 * Size, src.Length));
            }

            var bytes = Arrays.StringToByteArray(src);
            return new TraceId(bytes);
        }

        public static ITraceId GenerateRandomId(IRandomGenerator random)
        {
            var bytes = new byte[Size];
            do
            {
                random.NextBytes(bytes);
            }
            while (Arrays.Equals(bytes, InvalidTraceId.bytes));
            return new TraceId(bytes);
        }

        public void CopyBytesTo(byte[] dest, int destOffset)
        {
            Buffer.BlockCopy(bytes, 0, dest, destOffset, Size);
        }

        public string ToLowerBase16()
        {
            var bytes = Bytes;
            var result = Arrays.ByteArrayToString(bytes);
            return result;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is TraceId))
            {
                return false;
            }

            var that = (TraceId)obj;
            return Arrays.Equals(bytes, that.bytes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Arrays.GetHashCode(bytes);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "TraceId{"
               + "bytes=" + ToLowerBase16()
               + "}";
        }

        public int CompareTo(ITraceId other)
        {
            var that = other as TraceId;
            for (var i = 0; i < Size; i++)
            {
                if (bytes[i] != that.bytes[i])
                {
                    var b1 = (sbyte)bytes[i];
                    var b2 = (sbyte)that.bytes[i];

                    return b1 < b2 ? -1 : 1;
                }
            }

            return 0;
        }
    }
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class TraceIdTest
    {
        private static readonly byte[] FirstBytes =
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'a' };

        private static readonly byte[] SecondBytes =
            new byte[] { (byte)0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'A' };

        private static readonly ITraceId First = TraceId.FromBytes(FirstBytes);
        private static readonly ITraceId Second = TraceId.FromBytes(SecondBytes);

        [Fact]
        public void InvalidTraceId()
        {
            Assert.Equal(new byte[16], TraceId.INVALID.Bytes);
        }

        [Fact]
        public void IsValid()
        {
            Assert.False(TraceId.INVALID.IsValid);
            Assert.True(First.IsValid);
            Assert.True(Second.IsValid);
        }

        [Fact]
        public void Bytes()
        {
            Assert.Equal(FirstBytes, First.Bytes);
            Assert.Equal(SecondBytes, Second.Bytes);
        }

        [Fact]
        public void FromLowerBase16()
        {
            Assert.Equal(TraceId.INVALID, TraceId.FromLowerBase16("00000000000000000000000000000000"));
            Assert.Equal(First, TraceId.FromLowerBase16("00000000000000000000000000000061"));
            Assert.Equal(Second, TraceId.FromLowerBase16("ff000000000000000000000000000041"));
        }

        [Fact]
        public void ToLowerBase16()
        {
            Assert.Equal("00000000000000000000000000000000", TraceId.INVALID.ToLowerBase16());
            Assert.Equal("00000000000000000000000000000061", First.ToLowerBase16());
            Assert.Equal("ff000000000000000000000000000041", Second.ToLowerBase16());
        }

        [Fact]
        public void TraceId_CompareTo()
        {
            Assert.Equal(1, First.CompareTo(Second));
            Assert.Equal(-1, Second.CompareTo(First));
            Assert.Equal(0, First.CompareTo(TraceId.FromBytes(FirstBytes)));
        }

        [Fact]
        public void TraceId_EqualsAndHashCode()
        {
            // EqualsTester tester = new EqualsTester();
            // tester.addEqualityGroup(TraceId.INVALID, TraceId.INVALID);
            // tester.addEqualityGroup(first, TraceId.fromBytes(Arrays.copyOf(firstBytes, firstBytes.length)));
            // tester.addEqualityGroup(
            //    second, TraceId.fromBytes(Arrays.copyOf(secondBytes, secondBytes.length)));
            // tester.testEquals();
        }

        [Fact]
        public void TraceId_ToString()
        {
            Assert.Contains("00000000000000000000000000000000", TraceId.INVALID.ToString());
            Assert.Contains("00000000000000000000000000000061", First.ToString());
            Assert.Contains("ff000000000000000000000000000041", Second.ToString());
        }
    }
}

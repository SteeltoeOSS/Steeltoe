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
    public class SpanIdTest
    {
        private static readonly byte[] FirstBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] SecondBytes = new byte[] { (byte)0xFF, 0, 0, 0, 0, 0, 0, (byte)'A' };
        private static readonly ISpanId First = SpanId.FromBytes(FirstBytes);
        private static readonly ISpanId Second = SpanId.FromBytes(SecondBytes);

        [Fact]
        public void InvalidSpanId()
        {
            Assert.Equal(new byte[8], SpanId.INVALID.Bytes);
        }

        [Fact]
        public void IsValid()
        {
            Assert.False(SpanId.INVALID.IsValid);
            Assert.True(First.IsValid);
            Assert.True(Second.IsValid);
        }

        [Fact]
        public void FromLowerBase16()
        {
            Assert.Equal(SpanId.INVALID, SpanId.FromLowerBase16("0000000000000000"));
            Assert.Equal(First, SpanId.FromLowerBase16("0000000000000061"));
            Assert.Equal(Second, SpanId.FromLowerBase16("ff00000000000041"));
        }

        [Fact]
        public void ToLowerBase16()
        {
            Assert.Equal("0000000000000000", SpanId.INVALID.ToLowerBase16());
            Assert.Equal("0000000000000061", First.ToLowerBase16());
            Assert.Equal("ff00000000000041", Second.ToLowerBase16());
        }

        [Fact]
        public void Bytes()
        {
            Assert.Equal(FirstBytes, First.Bytes);
            Assert.Equal(SecondBytes, Second.Bytes);
        }

        [Fact]
        public void SpanId_CompareTo()
        {
            Assert.Equal(1, First.CompareTo(Second));
            Assert.Equal(-1, Second.CompareTo(First));
            Assert.Equal(0, First.CompareTo(SpanId.FromBytes(FirstBytes)));
        }

        ////[Fact]
        ////public void SpanId_EqualsAndHashCode()
        ////{
        ////    // EqualsTester tester = new EqualsTester();
        ////    // tester.addEqualityGroup(SpanId.INVALID, SpanId.INVALID);
        ////    // tester.addEqualityGroup(first, SpanId.fromBytes(Arrays.copyOf(firstBytes, firstBytes.length)));
        ////    // tester.addEqualityGroup(
        ////    //    second, SpanId.fromBytes(Arrays.copyOf(secondBytes, secondBytes.length)));
        ////    // tester.testEquals();
        ////}

        [Fact]
        public void SpanId_ToString()
        {
            Assert.Contains("0000000000000000", SpanId.INVALID.ToString());
            Assert.Contains("0000000000000061", First.ToString());
            Assert.Contains("ff00000000000041", Second.ToString());
        }
    }
}

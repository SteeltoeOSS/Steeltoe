using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class SpanIdTest
    {
        private static readonly byte[] firstBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] secondBytes = new byte[] { (byte)0xFF, 0, 0, 0, 0, 0, 0, (byte)'A' };
        private static readonly ISpanId first = SpanId.FromBytes(firstBytes);
        private static readonly ISpanId second = SpanId.FromBytes(secondBytes);

        [Fact]
        public void invalidSpanId()
        {
            Assert.Equal(new byte[8], SpanId.INVALID.Bytes);
        }

        [Fact]
        public void IsValid()
        {
            Assert.False(SpanId.INVALID.IsValid);
            Assert.True(first.IsValid);
            Assert.True(second.IsValid);
        }

        [Fact]
        public void FromLowerBase16()
        {
            Assert.Equal(SpanId.INVALID, SpanId.FromLowerBase16("0000000000000000"));
            Assert.Equal(first, SpanId.FromLowerBase16("0000000000000061"));
            Assert.Equal(second, SpanId.FromLowerBase16("ff00000000000041"));
        }

        [Fact]
        public void ToLowerBase16()
        {
            Assert.Equal("0000000000000000", SpanId.INVALID.ToLowerBase16());
            Assert.Equal("0000000000000061", first.ToLowerBase16());
            Assert.Equal("ff00000000000041", second.ToLowerBase16());
        }

        [Fact]
        public void Bytes()
        {
            Assert.Equal(firstBytes, first.Bytes);
            Assert.Equal(secondBytes, second.Bytes);

        }

    [Fact]
        public void SpanId_CompareTo()
        {
            Assert.Equal(1, first.CompareTo(second));
            Assert.Equal(-1, second.CompareTo(first));
            Assert.Equal(0, first.CompareTo(SpanId.FromBytes(firstBytes)));
        }

        [Fact]
        public void SpanId_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(SpanId.INVALID, SpanId.INVALID);
            //tester.addEqualityGroup(first, SpanId.fromBytes(Arrays.copyOf(firstBytes, firstBytes.length)));
            //tester.addEqualityGroup(
            //    second, SpanId.fromBytes(Arrays.copyOf(secondBytes, secondBytes.length)));
            //tester.testEquals();
        }

        [Fact]
        public void SpanId_ToString()
        {
            Assert.Contains("0000000000000000", SpanId.INVALID.ToString());
            Assert.Contains("0000000000000061", first.ToString());
            Assert.Contains("ff00000000000041", second.ToString());
        }
    }
}

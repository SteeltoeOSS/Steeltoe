using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TraceIdTest
    {
        private static readonly byte[] firstBytes =
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] secondBytes =
            new byte[] { (byte)0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'A' };
        private static readonly ITraceId first = TraceId.FromBytes(firstBytes);
        private static readonly ITraceId second = TraceId.FromBytes(secondBytes);

        [Fact]
        public void invalidTraceId()
        {
            Assert.Equal(new byte[16], TraceId.INVALID.Bytes);
        }

        [Fact]
        public void IsValid()
        {
            Assert.False(TraceId.INVALID.IsValid);
            Assert.True(first.IsValid);
            Assert.True(second.IsValid);
        }

        [Fact]
        public void Bytes()
        {
            Assert.Equal(firstBytes, first.Bytes);
            Assert.Equal(secondBytes, second.Bytes);
        }

        [Fact]
        public void FromLowerBase16()
        {
            Assert.Equal(TraceId.INVALID, TraceId.FromLowerBase16("00000000000000000000000000000000"));
            Assert.Equal(first, TraceId.FromLowerBase16("00000000000000000000000000000061"));
            Assert.Equal(second, TraceId.FromLowerBase16("ff000000000000000000000000000041"));
        }

        [Fact]
        public void ToLowerBase16()
        {
            Assert.Equal("00000000000000000000000000000000", TraceId.INVALID.ToLowerBase16());
            Assert.Equal("00000000000000000000000000000061", first.ToLowerBase16());
            Assert.Equal("ff000000000000000000000000000041", second.ToLowerBase16());
        }

        [Fact]
        public void TraceId_CompareTo()
        {
            Assert.Equal(1, first.CompareTo(second));
            Assert.Equal(-1, second.CompareTo(first));
            Assert.Equal(0, first.CompareTo(TraceId.FromBytes(firstBytes)));
        }

        [Fact]
        public void TraceId_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(TraceId.INVALID, TraceId.INVALID);
            //tester.addEqualityGroup(first, TraceId.fromBytes(Arrays.copyOf(firstBytes, firstBytes.length)));
            //tester.addEqualityGroup(
            //    second, TraceId.fromBytes(Arrays.copyOf(secondBytes, secondBytes.length)));
            //tester.testEquals();
        }

        [Fact]
        public void TraceId_ToString()
        {
            Assert.Contains("00000000000000000000000000000000", TraceId.INVALID.ToString());
            Assert.Contains("00000000000000000000000000000061", first.ToString());
            Assert.Contains("ff000000000000000000000000000041", second.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TraceOptionsTest
    {
        private static readonly byte[] firstBytes = { (byte)0xff };
        private static readonly byte[] secondBytes = { 1 };
        private static readonly byte[] thirdBytes = { 6 };

        [Fact]
        public void getOptions()
        {
            Assert.Equal(0, TraceOptions.DEFAULT.Options);
            Assert.Equal(0, TraceOptions.Builder().SetIsSampled(false).Build().Options);
            Assert.Equal(1, TraceOptions.Builder().SetIsSampled(true).Build().Options);
            Assert.Equal(0, TraceOptions.Builder().SetIsSampled(true).SetIsSampled(false).Build().Options);
            Assert.Equal(-1, TraceOptions.FromBytes(firstBytes).Options);
            Assert.Equal(1, TraceOptions.FromBytes(secondBytes).Options);
            Assert.Equal(6, TraceOptions.FromBytes(thirdBytes).Options);
        }

        [Fact]
        public void IsSampled()
        {
            Assert.False(TraceOptions.DEFAULT.IsSampled);
            Assert.True(TraceOptions.Builder().SetIsSampled(true).Build().IsSampled);
        }

        [Fact]
        public void ToFromBytes()
        {
            Assert.Equal(firstBytes, TraceOptions.FromBytes(firstBytes).Bytes);
            Assert.Equal(secondBytes, TraceOptions.FromBytes(secondBytes).Bytes);
            Assert.Equal(thirdBytes, TraceOptions.FromBytes(thirdBytes).Bytes);
        }

        [Fact]
        public void Builder_FromOptions()
        {
            Assert.Equal(6 | 1,
                    TraceOptions.Builder(TraceOptions.FromBytes(thirdBytes))
                        .SetIsSampled(true)
                        .Build()
                        .Options);
        }

        [Fact]
        public void traceOptions_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(TraceOptions.DEFAULT);
            //tester.addEqualityGroup(
            //    TraceOptions.FromBytes(secondBytes), TraceOptions.Builder().SetIsSampled(true).build());
            //tester.addEqualityGroup(TraceOptions.FromBytes(firstBytes));
            //tester.testEquals();
        }

        [Fact]
        public void traceOptions_ToString()
        {
            Assert.Contains("sampled=False", TraceOptions.DEFAULT.ToString());
            Assert.Contains("sampled=True", TraceOptions.Builder().SetIsSampled(true).Build().ToString());
        }
    }
}

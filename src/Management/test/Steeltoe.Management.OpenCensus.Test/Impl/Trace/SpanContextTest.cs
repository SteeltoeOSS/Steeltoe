using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class SpanContextTest
    {
        private static readonly byte[] firstTraceIdBytes =
         new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] secondTraceIdBytes =
            new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'0', 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] firstSpanIdBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] secondSpanIdBytes = new byte[] { (byte)'0', 0, 0, 0, 0, 0, 0, 0 };
        private static readonly ISpanContext first =
      SpanContext.Create(
          TraceId.FromBytes(firstTraceIdBytes),
          SpanId.FromBytes(firstSpanIdBytes),
          TraceOptions.DEFAULT);
        private static readonly ISpanContext second =
      SpanContext.Create(
          TraceId.FromBytes(secondTraceIdBytes),
          SpanId.FromBytes(secondSpanIdBytes),
          TraceOptions.Builder().SetIsSampled(true).Build());

        [Fact]
        public void InvalidSpanContext()
        {
            Assert.Equal(TraceId.INVALID, SpanContext.INVALID.TraceId);
            Assert.Equal(SpanId.INVALID, SpanContext.INVALID.SpanId);
            Assert.Equal(TraceOptions.DEFAULT, SpanContext.INVALID.TraceOptions);
        }

        [Fact]
        public void IsValid()
        {
            Assert.False(SpanContext.INVALID.IsValid);
            Assert.False(
                    SpanContext.Create(
                            TraceId.FromBytes(firstTraceIdBytes), SpanId.INVALID, TraceOptions.DEFAULT)
                        .IsValid);
            Assert.False(
                    SpanContext.Create(
                            TraceId.INVALID, SpanId.FromBytes(firstSpanIdBytes), TraceOptions.DEFAULT)
                        .IsValid);
            Assert.True(first.IsValid);
            Assert.True(second.IsValid);
        }

        [Fact]
        public void GetTraceId()
        {
            Assert.Equal(TraceId.FromBytes(firstTraceIdBytes), first.TraceId);
            Assert.Equal(TraceId.FromBytes(secondTraceIdBytes), second.TraceId);
        }

        [Fact]
        public void GetSpanId()
        {
            Assert.Equal(SpanId.FromBytes(firstSpanIdBytes), first.SpanId);
            Assert.Equal(SpanId.FromBytes(secondSpanIdBytes), second.SpanId);
        }

        [Fact]
        public void GetTraceOptions()
        {
            Assert.Equal(TraceOptions.DEFAULT, first.TraceOptions);
            Assert.Equal(TraceOptions.Builder().SetIsSampled(true).Build(), second.TraceOptions);
        }

        [Fact]
        public void SpanContext_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(
            //    first,
            //    SpanContext.create(
            //        TraceId.FromBytes(firstTraceIdBytes),
            //        SpanId.FromBytes(firstSpanIdBytes),
            //        TraceOptions.DEFAULT),
            //    SpanContext.create(
            //        TraceId.FromBytes(firstTraceIdBytes),
            //        SpanId.FromBytes(firstSpanIdBytes),
            //        TraceOptions.builder().setIsSampled(false).build()));
            //tester.addEqualityGroup(
            //    second,
            //    SpanContext.create(
            //        TraceId.FromBytes(secondTraceIdBytes),
            //        SpanId.FromBytes(secondSpanIdBytes),
            //        TraceOptions.builder().setIsSampled(true).build()));
            //tester.testEquals();
        }

        [Fact]
        public void SpanContext_ToString()
        {
            Assert.Contains(TraceId.FromBytes(firstTraceIdBytes).ToString(), first.ToString());
            Assert.Contains(SpanId.FromBytes(firstSpanIdBytes).ToString(), first.ToString());
            Assert.Contains(TraceOptions.DEFAULT.ToString(), first.ToString());
            Assert.Contains(TraceId.FromBytes(secondTraceIdBytes).ToString(), second.ToString());
            Assert.Contains(SpanId.FromBytes(secondSpanIdBytes).ToString(), second.ToString());
            Assert.Contains(TraceOptions.Builder().SetIsSampled(true).Build().ToString(), second.ToString());
        }
    }
}

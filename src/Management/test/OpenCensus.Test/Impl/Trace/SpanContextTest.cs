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
    public class SpanContextTest
    {
        private static readonly byte[] FirstTraceIdBytes =
         new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'a' };

        private static readonly byte[] SecondTraceIdBytes =
            new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'0', 0, 0, 0, 0, 0, 0, 0, 0 };

        private static readonly byte[] FirstSpanIdBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)'a' };
        private static readonly byte[] SecondSpanIdBytes = new byte[] { (byte)'0', 0, 0, 0, 0, 0, 0, 0 };
        private static readonly ISpanContext First =
      SpanContext.Create(
          TraceId.FromBytes(FirstTraceIdBytes),
          SpanId.FromBytes(FirstSpanIdBytes),
          TraceOptions.DEFAULT);

        private static readonly ISpanContext Second =
      SpanContext.Create(
          TraceId.FromBytes(SecondTraceIdBytes),
          SpanId.FromBytes(SecondSpanIdBytes),
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
                            TraceId.FromBytes(FirstTraceIdBytes), SpanId.INVALID, TraceOptions.DEFAULT)
                        .IsValid);
            Assert.False(
                    SpanContext.Create(
                            TraceId.INVALID, SpanId.FromBytes(FirstSpanIdBytes), TraceOptions.DEFAULT)
                        .IsValid);
            Assert.True(First.IsValid);
            Assert.True(Second.IsValid);
        }

        [Fact]
        public void GetTraceId()
        {
            Assert.Equal(TraceId.FromBytes(FirstTraceIdBytes), First.TraceId);
            Assert.Equal(TraceId.FromBytes(SecondTraceIdBytes), Second.TraceId);
        }

        [Fact]
        public void GetSpanId()
        {
            Assert.Equal(SpanId.FromBytes(FirstSpanIdBytes), First.SpanId);
            Assert.Equal(SpanId.FromBytes(SecondSpanIdBytes), Second.SpanId);
        }

        [Fact]
        public void GetTraceOptions()
        {
            Assert.Equal(TraceOptions.DEFAULT, First.TraceOptions);
            Assert.Equal(TraceOptions.Builder().SetIsSampled(true).Build(), Second.TraceOptions);
        }

        [Fact]
        public void SpanContext_EqualsAndHashCode()
        {
            // EqualsTester tester = new EqualsTester();
            // tester.addEqualityGroup(
            //    first,
            //    SpanContext.create(
            //        TraceId.FromBytes(firstTraceIdBytes),
            //        SpanId.FromBytes(firstSpanIdBytes),
            //        TraceOptions.DEFAULT),
            //    SpanContext.create(
            //        TraceId.FromBytes(firstTraceIdBytes),
            //        SpanId.FromBytes(firstSpanIdBytes),
            //        TraceOptions.builder().setIsSampled(false).build()));
            // tester.addEqualityGroup(
            //    second,
            //    SpanContext.create(
            //        TraceId.FromBytes(secondTraceIdBytes),
            //        SpanId.FromBytes(secondSpanIdBytes),
            //        TraceOptions.builder().setIsSampled(true).build()));
            // tester.testEquals();
        }

        [Fact]
        public void SpanContext_ToString()
        {
            Assert.Contains(TraceId.FromBytes(FirstTraceIdBytes).ToString(), First.ToString());
            Assert.Contains(SpanId.FromBytes(FirstSpanIdBytes).ToString(), First.ToString());
            Assert.Contains(TraceOptions.DEFAULT.ToString(), First.ToString());
            Assert.Contains(TraceId.FromBytes(SecondTraceIdBytes).ToString(), Second.ToString());
            Assert.Contains(SpanId.FromBytes(SecondSpanIdBytes).ToString(), Second.ToString());
            Assert.Contains(TraceOptions.Builder().SetIsSampled(true).Build().ToString(), Second.ToString());
        }
    }
}

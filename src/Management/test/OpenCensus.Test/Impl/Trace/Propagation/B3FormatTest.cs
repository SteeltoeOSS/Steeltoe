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
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    [Obsolete]
    public class B3FormatTest
    {
        private static readonly string TRACE_ID_BASE16 = "ff000000000000000000000000000041";
        private static readonly ITraceId TRACE_ID = TraceId.FromLowerBase16(TRACE_ID_BASE16);
        private static readonly string TRACE_ID_BASE16_EIGHT_BYTES = "0000000000000041";
        private static readonly ITraceId TRACE_ID_EIGHT_BYTES = TraceId.FromLowerBase16("0000000000000000" + TRACE_ID_BASE16_EIGHT_BYTES);
        private static readonly string SPAN_ID_BASE16 = "ff00000000000041";
        private static readonly ISpanId SPAN_ID = SpanId.FromLowerBase16(SPAN_ID_BASE16);
        private static readonly byte[] TRACE_OPTIONS_BYTES = new byte[] { 1 };
        private static readonly TraceOptions TRACE_OPTIONS = TraceOptions.FromBytes(TRACE_OPTIONS_BYTES);
        private readonly B3Format b3Format = new B3Format();

#pragma warning disable SA1204 // Static elements must appear before instance elements
        private static readonly ISetter<IDictionary<string, string>> Setter = new TestSetter();
#pragma warning restore SA1204 // Static elements must appear before instance elements
        private static readonly IGetter<IDictionary<string, string>> Getter = new TestGetter();
        private ITestOutputHelper _output;

        public B3FormatTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Serialize_SampledContext()
        {
            IDictionary<string, string> carrier = new Dictionary<string, string>();
            b3Format.Inject(SpanContext.Create(TRACE_ID, SPAN_ID, TRACE_OPTIONS), carrier, Setter);
            ContainsExactly(carrier, new Dictionary<string, string>() { { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 }, { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }, { B3Format.X_B3_SAMPLED, "1" } });
        }

        [Fact]
        public void Serialize_NotSampledContext()
        {
            IDictionary<string, string> carrier = new Dictionary<string, string>();
            var context = SpanContext.Create(TRACE_ID, SPAN_ID, TraceOptions.DEFAULT);
            _output.WriteLine(context.ToString());
            b3Format.Inject(context, carrier, Setter);
            ContainsExactly(carrier, new Dictionary<string, string>() { { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 }, { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 } });
        }

        [Fact]
        public void ParseMissingSampledAndMissingFlag()
        {
            IDictionary<string, string> headersNotSampled = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }
            };
            ISpanContext spanContext = SpanContext.Create(TRACE_ID, SPAN_ID, TraceOptions.DEFAULT);
            Assert.Equal(spanContext, b3Format.Extract(headersNotSampled, Getter));
        }

        [Fact]
        public void ParseSampled()
        {
            IDictionary<string, string> headersSampled = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 },
                { B3Format.X_B3_SAMPLED, "1" }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID, SPAN_ID, TRACE_OPTIONS), b3Format.Extract(headersSampled, Getter));
        }

        [Fact]
        public void ParseZeroSampled()
        {
            IDictionary<string, string> headersNotSampled = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 },
                { B3Format.X_B3_SAMPLED, "0" }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID, SPAN_ID, TraceOptions.DEFAULT), b3Format.Extract(headersNotSampled, Getter));
        }

        [Fact]
        public void ParseFlag()
        {
            IDictionary<string, string> headersFlagSampled = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 },
                { B3Format.X_B3_FLAGS, "1" }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID, SPAN_ID, TRACE_OPTIONS), b3Format.Extract(headersFlagSampled, Getter));
        }

        [Fact]
        public void ParseZeroFlag()
        {
            IDictionary<string, string> headersFlagNotSampled = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 },
                { B3Format.X_B3_FLAGS, "0" }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID, SPAN_ID, TraceOptions.DEFAULT), b3Format.Extract(headersFlagNotSampled, Getter));
        }

        [Fact]
        public void ParseEightBytesTraceId()
        {
            IDictionary<string, string> headersEightBytes = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16_EIGHT_BYTES },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 },
                { B3Format.X_B3_SAMPLED, "1" }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID_EIGHT_BYTES, SPAN_ID, TRACE_OPTIONS), b3Format.Extract(headersEightBytes, Getter));
        }

        [Fact]
        public void ParseEightBytesTraceId_NotSampledSpanContext()
        {
            IDictionary<string, string> headersEightBytes = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16_EIGHT_BYTES },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }
            };
            Assert.Equal(SpanContext.Create(TRACE_ID_EIGHT_BYTES, SPAN_ID, TraceOptions.DEFAULT), b3Format.Extract(headersEightBytes, Getter));
        }

        [Fact]
        public void ParseInvalidTraceId()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, "abcdefghijklmnop" },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void ParseInvalidTraceId_Size()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, "0123456789abcdef00" },
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void ParseMissingTraceId()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_SPAN_ID, SPAN_ID_BASE16 }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void ParseInvalidSpanId()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, "abcdefghijklmnop" }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void ParseInvalidSpanId_Size()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 },
                { B3Format.X_B3_SPAN_ID, "0123456789abcdef00" }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void ParseMissingSpanId()
        {
            IDictionary<string, string> invalidHeaders = new Dictionary<string, string>
            {
                { B3Format.X_B3_TRACE_ID, TRACE_ID_BASE16 }
            };
            Assert.Throws<SpanContextParseException>(() => b3Format.Extract(invalidHeaders, Getter));
        }

        [Fact]
        public void Fields_list()
        {
            ContainsExactly(
                b3Format.Fields,
                new List<string>() { B3Format.X_B3_TRACE_ID, B3Format.X_B3_SPAN_ID, B3Format.X_B3_PARENT_SPAN_ID, B3Format.X_B3_SAMPLED, B3Format.X_B3_FLAGS });
        }

        private void ContainsExactly(IList<string> list, List<string> items)
        {
            Assert.Equal(items.Count, list.Count);
            foreach (var item in items)
            {
                Assert.Contains(item, list);
            }
        }

        private void ContainsExactly(IDictionary<string, string> dict, IDictionary<string, string> items)
        {
            foreach (var d in dict)
            {
                _output.WriteLine(d.Key + "=" + d.Value);
            }

            Assert.Equal(items.Count, dict.Count);
            foreach (var item in items)
            {
                Assert.Contains(item, dict);
            }
        }

        private class TestSetter : ISetter<IDictionary<string, string>>
        {
            public void Put(IDictionary<string, string> carrier, string key, string value)
            {
                carrier[key] = value;
            }
        }

        private class TestGetter : IGetter<IDictionary<string, string>>
        {
            public string Get(IDictionary<string, string> carrier, string key)
            {
                carrier.TryGetValue(key, out string result);
                return result;
            }
        }
    }
}

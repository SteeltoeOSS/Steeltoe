// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using OpenCensus.Trace;
using Steeltoe.Extensions.Logging;
using System.Text;

namespace Steeltoe.Management.Tracing
{
    public class TracingLogProcessor : IDynamicMessageProcessor
    {
        private ITracing tracing;
        private ITracingOptions options;

        public TracingLogProcessor(ITracingOptions options, ITracing tracing)
        {
            this.options = options;
            this.tracing = tracing;
        }

        public string Process(string inputLogMessage)
        {
            Span currentSpan = tracing.Tracer?.CurrentSpan as Span;
            if (currentSpan != null)
            {
                var context = currentSpan.Context;
                if (context != null)
                {
                    StringBuilder sb = new StringBuilder(" [");
                    sb.Append(options.Name);
                    sb.Append(",");

                    var traceId = context.TraceId.ToLowerBase16();
                    if (traceId.Length > 16 && options.UseShortTraceIds)
                    {
                        traceId = traceId.Substring(traceId.Length - 16, 16);
                    }

                    sb.Append(traceId);
                    sb.Append(",");

                    sb.Append(context.SpanId.ToLowerBase16());
                    sb.Append(",");

                    if (currentSpan.ParentSpanId != null)
                    {
                        sb.Append(currentSpan.ParentSpanId.ToLowerBase16());
                    }

                    sb.Append(",");

                    if (currentSpan.Options.HasFlag(SpanOptions.RECORD_EVENTS))
                    {
                        sb.Append("true");
                    }
                    else
                    {
                        sb.Append("false");
                    }

                    sb.Append("] ");
                    return sb.ToString() + inputLogMessage;
                }
            }

            return inputLogMessage;
        }
    }
}

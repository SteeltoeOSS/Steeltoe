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

using Moq;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Testing.Export;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    [Obsolete]
    public class SpanExporterTest
    {
        private static readonly string SPAN_NAME_1 = "MySpanName/1";
        private static readonly string SPAN_NAME_2 = "MySpanName/2";
        private readonly RandomGenerator random = new RandomGenerator(1234);
        private readonly ISpanContext sampledSpanContext;
        private readonly ISpanContext notSampledSpanContext;
        private readonly ISpanExporter spanExporter = SpanExporter.Create(4, Duration.Create(1, 0));
        private readonly IRunningSpanStore runningSpanStore = new InProcessRunningSpanStore();
        private readonly IStartEndHandler startEndHandler;
        private readonly SpanOptions recordSpanOptions = SpanOptions.RECORD_EVENTS;
#pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields
        private readonly TestHandler serviceHandler = new TestHandler();
#pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
        private readonly IHandler mockServiceHandler = Mock.Of<IHandler>();

        public SpanExporterTest()
        {
            sampledSpanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.Builder().SetIsSampled(true).Build());
            notSampledSpanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.DEFAULT);
            startEndHandler = new StartEndHandler(spanExporter, runningSpanStore, null, new SimpleEventQueue());

            spanExporter.RegisterHandler("test.service", serviceHandler);
        }

        private Span CreateSampledEndedSpan(string spanName)
        {
            ISpan span =
                Span.StartSpan(
                    sampledSpanContext,
                    recordSpanOptions,
                    spanName,
                    null,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    null,
                    DateTimeOffsetClock.INSTANCE);
            span.End();
            return span as Span;
        }

        private Span CreateNotSampledEndedSpan(string spanName)
        {
            ISpan span =
                Span.StartSpan(
                    notSampledSpanContext,
                    recordSpanOptions,
                    spanName,
                    null,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    null,
                    DateTimeOffsetClock.INSTANCE);
            span.End();
            return span as Span;
        }

        [Fact]
#pragma warning disable SA1202 // Elements must be ordered by access
        public void ExportDifferentSampledSpans()
#pragma warning restore SA1202 // Elements must be ordered by access
        {
            Span span1 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span2 = CreateSampledEndedSpan(SPAN_NAME_2);
            IList<ISpanData> exported = serviceHandler.WaitForExport(2);
            Assert.Equal(2, exported.Count);
            Assert.Contains(span1.ToSpanData(), exported);
            Assert.Contains(span2.ToSpanData(), exported);
        }

        [Fact]
        public void ExportMoreSpansThanTheBufferSize()
        {
            Span span1 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span2 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span3 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span4 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span5 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span6 = CreateSampledEndedSpan(SPAN_NAME_1);
            IList<ISpanData> exported = serviceHandler.WaitForExport(6);
            Assert.Equal(6, exported.Count);
            Assert.Contains(span1.ToSpanData(), exported);
            Assert.Contains(span2.ToSpanData(), exported);
            Assert.Contains(span3.ToSpanData(), exported);
            Assert.Contains(span4.ToSpanData(), exported);
            Assert.Contains(span5.ToSpanData(), exported);
            Assert.Contains(span6.ToSpanData(), exported);
        }

        [Fact]
        public void InterruptWorkerThreadStops()
        {
            Thread serviceExporterThread = ((SpanExporter)spanExporter).ServiceExporterThread;
            spanExporter.Dispose();

            // serviceExporterThread.Interrupt();
            // Test that the worker thread will stop.
            serviceExporterThread.Join();
        }

        [Fact]
        public void ServiceHandlerThrowsException()
        {
            var mockHandler = Mock.Get<IHandler>(mockServiceHandler);
            mockHandler.Setup((h) => h.Export(It.IsAny<IList<ISpanData>>())).Throws(new ArgumentException("No export for you."));

            // doThrow(new IllegalArgumentException("No export for you."))
            //    .when(mockServiceHandler)
            //    .export(anyListOf(SpanData));
            spanExporter.RegisterHandler("mock.service", mockServiceHandler);
            Span span1 = CreateSampledEndedSpan(SPAN_NAME_1);
            IList<ISpanData> exported = serviceHandler.WaitForExport(1);
            Assert.Equal(1, exported.Count);
            Assert.Contains(span1.ToSpanData(), exported);

            // assertThat(exported).containsExactly(span1.toSpanData());
            // Continue to export after the exception was received.
            Span span2 = CreateSampledEndedSpan(SPAN_NAME_1);
            exported = serviceHandler.WaitForExport(1);
            Assert.Equal(1, exported.Count);
            Assert.Contains(span2.ToSpanData(), exported);

            // assertThat(exported).containsExactly(span2.toSpanData());
        }

        [Fact]
        public void ExportSpansToMultipleServices()
        {
            TestHandler serviceHandler2 = new TestHandler();
            spanExporter.RegisterHandler("test.service2", serviceHandler2);
            Span span1 = CreateSampledEndedSpan(SPAN_NAME_1);
            Span span2 = CreateSampledEndedSpan(SPAN_NAME_2);
            IList<ISpanData> exported1 = serviceHandler.WaitForExport(2);
            IList<ISpanData> exported2 = serviceHandler2.WaitForExport(2);
            Assert.Equal(2, exported1.Count);
            Assert.Contains(span1.ToSpanData(), exported1);
            Assert.Contains(span2.ToSpanData(), exported1);
            Assert.Equal(2, exported2.Count);
            Assert.Contains(span1.ToSpanData(), exported2);
            Assert.Contains(span2.ToSpanData(), exported2);
        }

        [Fact]
        public void ExportNotSampledSpans()
        {
            Span span1 = CreateNotSampledEndedSpan(SPAN_NAME_1);
            Span span2 = CreateSampledEndedSpan(SPAN_NAME_2);

            // Spans are recorded and exported in the same order as they are ended, we test that a non
            // sampled span is not exported by creating and ending a sampled span after a non sampled span
            // and checking that the first exported span is the sampled span (the non sampled did not get
            // exported).
            IList<ISpanData> exported = serviceHandler.WaitForExport(1);

            // Need to check this because otherwise the variable span1 is unused, other option is to not
            // have a span1 variable.
            Assert.Equal(1, exported.Count);
            Assert.DoesNotContain(span1.ToSpanData(), exported);
            Assert.Contains(span2.ToSpanData(), exported);
        }
    }
}

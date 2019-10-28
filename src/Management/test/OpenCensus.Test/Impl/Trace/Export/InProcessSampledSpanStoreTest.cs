﻿// <copyright file="InProcessSampledSpanStoreTest.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace.Export.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenCensus.Common;
    using OpenCensus.Internal;
    using OpenCensus.Testing.Common;
    using OpenCensus.Trace.Config;
    using OpenCensus.Trace.Internal;
    using OpenCensus.Utils;
    using Xunit;

    public class InProcessSampledSpanStoreTest
    {
        private static readonly String REGISTERED_SPAN_NAME = "MySpanName/1";
        private static readonly String NOT_REGISTERED_SPAN_NAME = "MySpanName/2";
        private static readonly long NUM_NANOS_PER_SECOND = 1000000000L;
        private readonly RandomGenerator random = new RandomGenerator(1234);
        private readonly ISpanContext sampledSpanContext;

        private readonly ISpanContext notSampledSpanContext;

        private readonly ISpanId parentSpanId;
        private readonly SpanOptions recordSpanOptions = SpanOptions.RecordEvents;
        private readonly TestClock testClock = TestClock.Create(Timestamp.Create(12345, 54321));
        private readonly InProcessSampledSpanStore sampleStore = new InProcessSampledSpanStore(new SimpleEventQueue());

        private readonly IStartEndHandler startEndHandler;


        public InProcessSampledSpanStoreTest()
        {
            sampledSpanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.Builder().SetIsSampled(true).Build(), Tracestate.Empty);
            notSampledSpanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.Default, Tracestate.Empty);
            parentSpanId = SpanId.GenerateRandomId(random);
            startEndHandler = new TestStartEndHandler(sampleStore);
            sampleStore.RegisterSpanNamesForCollection(new List<string>() { REGISTERED_SPAN_NAME });
        }



        [Fact]
        public void AddSpansWithRegisteredNamesInAllLatencyBuckets()
        {
            AddSpanNameToAllLatencyBuckets(REGISTERED_SPAN_NAME);
            IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary = sampleStore.Summary.PerSpanNameSummary;
            Assert.Equal(1, perSpanNameSummary.Count);
            IDictionary<ISampledLatencyBucketBoundaries, int> latencyBucketsSummaries = perSpanNameSummary[REGISTERED_SPAN_NAME].NumbersOfLatencySampledSpans;
            Assert.Equal(LatencyBucketBoundaries.Values.Count, latencyBucketsSummaries.Count);
            foreach (var it in latencyBucketsSummaries)
            {
                Assert.Equal(2, it.Value);
            }
        }

        [Fact]
        public void AddSpansWithoutRegisteredNamesInAllLatencyBuckets()
        {
            AddSpanNameToAllLatencyBuckets(NOT_REGISTERED_SPAN_NAME);
            IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary = sampleStore.Summary.PerSpanNameSummary;
            Assert.Equal(1, perSpanNameSummary.Count);
            Assert.False(perSpanNameSummary.ContainsKey(NOT_REGISTERED_SPAN_NAME));
        }

        [Fact]
        public void RegisterUnregisterAndListSpanNames()
        {
            Assert.Contains(REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Equal(1, sampleStore.RegisteredSpanNamesForCollection.Count);

            sampleStore.RegisterSpanNamesForCollection(new List<string>() { NOT_REGISTERED_SPAN_NAME });

            Assert.Contains(REGISTERED_SPAN_NAME,  sampleStore.RegisteredSpanNamesForCollection);
            Assert.Contains(NOT_REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Equal(2, sampleStore.RegisteredSpanNamesForCollection.Count);

            sampleStore.UnregisterSpanNamesForCollection(new List<string>() { NOT_REGISTERED_SPAN_NAME });

            Assert.Contains(REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Equal(1, sampleStore.RegisteredSpanNamesForCollection.Count);
        }

        [Fact]
        public void RegisterSpanNamesViaSpanBuilderOption()
        {
            Assert.Contains(REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Equal(1, sampleStore.RegisteredSpanNamesForCollection.Count);

            CreateSampledSpan(NOT_REGISTERED_SPAN_NAME).End(EndSpanOptions.Builder().SetSampleToLocalSpanStore(true).Build());

            Assert.Contains(REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Contains(NOT_REGISTERED_SPAN_NAME, sampleStore.RegisteredSpanNamesForCollection);
            Assert.Equal(2, sampleStore.RegisteredSpanNamesForCollection.Count);

        }

        [Fact]
        public void AddSpansWithRegisteredNamesInAllErrorBuckets()
        {
            AddSpanNameToAllErrorBuckets(REGISTERED_SPAN_NAME);
            IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary = sampleStore.Summary.PerSpanNameSummary;
            Assert.Equal(1, perSpanNameSummary.Count);
            IDictionary<CanonicalCode, int> errorBucketsSummaries = perSpanNameSummary[REGISTERED_SPAN_NAME].NumbersOfErrorSampledSpans;
            var ccCount = Enum.GetValues(typeof(CanonicalCode)).Cast<CanonicalCode>().Count();
            Assert.Equal(ccCount - 1, errorBucketsSummaries.Count);
            foreach (var it in errorBucketsSummaries)
            {
                Assert.Equal(2, it.Value);
            }
        }

        [Fact]
        public void AddSpansWithoutRegisteredNamesInAllErrorBuckets()
        {
            AddSpanNameToAllErrorBuckets(NOT_REGISTERED_SPAN_NAME);
            IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary = sampleStore.Summary.PerSpanNameSummary;
            Assert.Equal(1, perSpanNameSummary.Count);
            Assert.False(perSpanNameSummary.ContainsKey(NOT_REGISTERED_SPAN_NAME));
        }

        [Fact]
        public void GetErrorSampledSpans()
        {
            Span span = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span.End(EndSpanOptions.Builder().SetStatus(Status.Cancelled).Build());
            var samples =
                sampleStore.GetErrorSampledSpans(
                    SampledSpanStoreErrorFilter.Create(REGISTERED_SPAN_NAME, CanonicalCode.Cancelled, 0));
            Assert.Single(samples);
            Assert.Contains(span.ToSpanData(), samples);
        }

        [Fact]
        public void GetErrorSampledSpans_MaxSpansToReturn()
        {
            Span span1 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span1.End(EndSpanOptions.Builder().SetStatus(Status.Cancelled).Build());
            // Advance time to allow other spans to be sampled.
            testClock.AdvanceTime(Duration.Create(5, 0));
            Span span2 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span2.End(EndSpanOptions.Builder().SetStatus(Status.Cancelled).Build());
            var samples =
                sampleStore.GetErrorSampledSpans(
                    SampledSpanStoreErrorFilter.Create(REGISTERED_SPAN_NAME, CanonicalCode.Cancelled, 1));
            Assert.Single(samples);
            // No order guaranteed so one of the spans should be in the list.
            Assert.True(samples.Contains(span1.ToSpanData()) || samples.Contains(span2.ToSpanData()));
        }

        [Fact]
        public void GetErrorSampledSpans_NullCode()
        {
            Span span1 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span1.End(EndSpanOptions.Builder().SetStatus(Status.Cancelled).Build());
            Span span2 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span2.End(EndSpanOptions.Builder().SetStatus(Status.Unknown).Build());
            var samples =
                sampleStore.GetErrorSampledSpans(SampledSpanStoreErrorFilter.Create(REGISTERED_SPAN_NAME, null, 0));
            Assert.Equal(2, samples.Count());
            Assert.Contains(span1.ToSpanData(), samples);
            Assert.Contains(span2.ToSpanData(), samples);
        }

        [Fact]
        public void GetErrorSampledSpans_NullCode_MaxSpansToReturn()
        {
            Span span1 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span1.End(EndSpanOptions.Builder().SetStatus(Status.Cancelled).Build());
            Span span2 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 1000));
            span2.End(EndSpanOptions.Builder().SetStatus(Status.Unknown).Build());
            var samples =
                sampleStore.GetErrorSampledSpans(SampledSpanStoreErrorFilter.Create(REGISTERED_SPAN_NAME, null, 1));
            Assert.Single(samples);
            Assert.True(samples.Contains(span1.ToSpanData()) || samples.Contains(span2.ToSpanData()));
        }

        [Fact]
        public void GetLatencySampledSpans()
        {
            Span span = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 20000)); // 20 microseconds
            span.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(
                        REGISTERED_SPAN_NAME,
                        15000,
                        25000,
                        0));
            Assert.Single(samples);
            Assert.Contains(span.ToSpanData(), samples);
        }

        [Fact]
        public void GetLatencySampledSpans_ExclusiveUpperBound()
        {
            Span span = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 20000)); // 20 microseconds
            span.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(
                        REGISTERED_SPAN_NAME,
                        15000,
                        20000,
                        0));
            Assert.Empty(samples);
        }

        [Fact]
        public void GetLatencySampledSpans_InclusiveLowerBound()
        {
            Span span = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 20000)); // 20 microseconds
            span.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(
                        REGISTERED_SPAN_NAME,
                        15000,
                        25000,
                        0));
            Assert.Single(samples);
            Assert.Contains(span.ToSpanData(), samples);
        }

        [Fact]
        public void GetLatencySampledSpans_QueryBetweenMultipleBuckets()
        {
            Span span1 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 20000)); // 20 microseconds
            span1.End();
            // Advance time to allow other spans to be sampled.
            testClock.AdvanceTime(Duration.Create(5, 0));
            Span span2 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 200000)); // 200 microseconds
            span2.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(
                        REGISTERED_SPAN_NAME,
                        15000,
                        250000,
                        0));
            Assert.Equal(2, samples.Count());
            Assert.Contains(span1.ToSpanData(), samples);
            Assert.Contains(span2.ToSpanData(), samples);
        }

        [Fact]
        public void GetLatencySampledSpans_MaxSpansToReturn()
        {
            Span span1 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 20000)); // 20 microseconds
            span1.End();
            // Advance time to allow other spans to be sampled.
            testClock.AdvanceTime(Duration.Create(5, 0));
            Span span2 = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, 200000)); // 200 microseconds
            span2.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(
                        REGISTERED_SPAN_NAME,
                        15000,
                        250000,
                        1));
            Assert.Single(samples);
            Assert.Contains(span1.ToSpanData(), samples);
        }

        [Fact]
        public void IgnoreNegativeSpanLatency()
        {
            Span span = CreateSampledSpan(REGISTERED_SPAN_NAME) as Span;
            testClock.AdvanceTime(Duration.Create(0, -20000));
            span.End();
            var samples =
                sampleStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create(REGISTERED_SPAN_NAME, 0, Int64.MaxValue, 0));
            Assert.Empty(samples);
        }

        private ISpan CreateSampledSpan(string spanName)
        {
            return Span.StartSpan(
                sampledSpanContext,
                recordSpanOptions,
                spanName,
                parentSpanId,
                false,
                TraceParams.Default,
                startEndHandler,
                null,
                testClock);
        }

        private ISpan CreateNotSampledSpan(string spanName)
        {
            return Span.StartSpan(
                notSampledSpanContext,
                recordSpanOptions,
                spanName,
                parentSpanId,
                false,
                TraceParams.Default,
                startEndHandler,
                null,
                testClock);
        }

        private void AddSpanNameToAllLatencyBuckets(string spanName)
        {
            foreach (LatencyBucketBoundaries boundaries in LatencyBucketBoundaries.Values)
            {
                ISpan sampledSpan = CreateSampledSpan(spanName);
                ISpan notSampledSpan = CreateNotSampledSpan(spanName);
                if (boundaries.LatencyLowerNs < NUM_NANOS_PER_SECOND)
                {
                    testClock.AdvanceTime(Duration.Create(0, (int)boundaries.LatencyLowerNs));
                }
                else
                {
                    testClock.AdvanceTime(
                        Duration.Create(
                            boundaries.LatencyLowerNs / NUM_NANOS_PER_SECOND,
                            (int)(boundaries.LatencyLowerNs % NUM_NANOS_PER_SECOND)));
                }
                sampledSpan.End();
                notSampledSpan.End();
            }
        }

        private void AddSpanNameToAllErrorBuckets(String spanName)
        {
            foreach (CanonicalCode code in Enum.GetValues(typeof(CanonicalCode)).Cast<CanonicalCode>())
            {
                if (code != CanonicalCode.Ok)
                {
                    ISpan sampledSpan = CreateSampledSpan(spanName);
                    ISpan notSampledSpan = CreateNotSampledSpan(spanName);
                    testClock.AdvanceTime(Duration.Create(0, 1000));
                    sampledSpan.End(EndSpanOptions.Builder().SetStatus(code.ToStatus()).Build());
                    notSampledSpan.End(EndSpanOptions.Builder().SetStatus(code.ToStatus()).Build());
                }
            }
        }

        class TestStartEndHandler : IStartEndHandler
        {
            InProcessSampledSpanStore sampleStore;

            public TestStartEndHandler(InProcessSampledSpanStore store)
            {
                sampleStore = store;
            }

            public void OnStart(ISpan span)
            {
                // Do nothing.
            }

            public void OnEnd(ISpan span)
            {
                sampleStore.ConsiderForSampling(span);
            }
        }
    }
}

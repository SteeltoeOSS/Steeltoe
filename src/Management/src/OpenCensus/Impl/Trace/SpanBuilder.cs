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

using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class SpanBuilder : SpanBuilderBase
    {
        private SpanBuilderOptions Options { get; set; }

        private string Name { get; set; }

        private ISpan Parent { get; set; }

        private ISpanContext RemoteParentSpanContext { get; set; }

        private ISampler Sampler { get; set; }

        private IList<ISpan> ParentLinks { get; set; } = new List<ISpan>();

        private bool RecordEvents { get; set; }

        internal static ISpanBuilder CreateWithParent(string spanName, ISpan parent, SpanBuilderOptions options)
        {
            return new SpanBuilder(spanName, options, null, parent);
        }

        internal static ISpanBuilder CreateWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext, SpanBuilderOptions options)
        {
            return new SpanBuilder(spanName, options, remoteParentSpanContext, null);
        }

        internal SpanBuilder()
        {
        }

        private SpanBuilder(string name, SpanBuilderOptions options, ISpanContext remoteParentSpanContext = null, ISpan parent = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Parent = parent;
            RemoteParentSpanContext = remoteParentSpanContext;
            Options = options;
        }

        private ISpan StartSpanInternal(
             ISpanContext parent,
             bool hasRemoteParent,
             string name,
             ISampler sampler,
             IList<ISpan> parentLinks,
             bool recordEvents,
             ITimestampConverter timestampConverter)
        {
            ITraceParams activeTraceParams = Options.TraceConfig.ActiveTraceParams;
            IRandomGenerator random = Options.RandomHandler;
            ITraceId traceId;
            ISpanId spanId = SpanId.GenerateRandomId(random);
            ISpanId parentSpanId = null;
            TraceOptionsBuilder traceOptionsBuilder;
            if (parent == null || !parent.IsValid)
            {
                // New root span.
                traceId = TraceId.GenerateRandomId(random);
                traceOptionsBuilder = TraceOptions.Builder();

                // This is a root span so no remote or local parent.
                // hasRemoteParent = null;
                hasRemoteParent = false;
            }
            else
            {
                // New child span.
                traceId = parent.TraceId;
                parentSpanId = parent.SpanId;
                traceOptionsBuilder = TraceOptions.Builder(parent.TraceOptions);
            }

            traceOptionsBuilder.SetIsSampled(
                 MakeSamplingDecision(
                    parent,
                    hasRemoteParent,
                    name,
                    sampler,
                    parentLinks,
                    traceId,
                    spanId,
                    activeTraceParams));
            TraceOptions traceOptions = traceOptionsBuilder.Build();
            SpanOptions spanOptions = SpanOptions.NONE;

            if (traceOptions.IsSampled || recordEvents)
            {
                spanOptions = SpanOptions.RECORD_EVENTS;
            }

            ISpan span = Span.StartSpan(
                        SpanContext.Create(traceId, spanId, traceOptions),
                        spanOptions,
                        name,
                        parentSpanId,
                        hasRemoteParent,
                        activeTraceParams,
                        Options.StartEndHandler,
                        timestampConverter,
                        Options.Clock);
            LinkSpans(span, parentLinks);
            return span;
        }

        public override ISpan StartSpan()
        {
            ISpanContext parentContext = RemoteParentSpanContext;
            bool hasRemoteParent = true;
            ITimestampConverter timestampConverter = null;
            if (RemoteParentSpanContext == null)
            {
                // This is not a child of a remote Span. Get the parent SpanContext from the parent Span if
                // any.
                ISpan parent = this.Parent;
                hasRemoteParent = false;
                if (parent != null)
                {
                    parentContext = parent.Context;

                    // Pass the timestamp converter from the parent to ensure that the recorded events are in
                    // the right order. Implementation uses System.nanoTime() which is monotonically increasing.
                    if (parent is Span)
                    {
                        timestampConverter = ((Span)parent).TimestampConverter;
                    }
                }
                else
                {
                    hasRemoteParent = false;
                }
            }

            return StartSpanInternal(
                parentContext,
                hasRemoteParent,
                Name,
                Sampler,
                ParentLinks,
                RecordEvents,
                timestampConverter);
        }

        public override ISpanBuilder SetSampler(ISampler sampler)
        {
            if (sampler == null)
            {
                throw new ArgumentNullException(nameof(sampler));
            }

            this.Sampler = sampler;
            return this;
        }

        public override ISpanBuilder SetParentLinks(IList<ISpan> parentLinks)
        {
            if (parentLinks == null)
            {
                throw new ArgumentNullException(nameof(parentLinks));
            }

            this.ParentLinks = parentLinks;
            return this;
        }

        public override ISpanBuilder SetRecordEvents(bool recordEvents)
        {
            this.RecordEvents = recordEvents;
            return this;
        }

        private static bool IsAnyParentLinkSampled(IList<ISpan> parentLinks)
        {
            foreach (ISpan parentLink in parentLinks)
            {
                if (parentLink.Context.TraceOptions.IsSampled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LinkSpans(ISpan span, IList<ISpan> parentLinks)
        {
            if (parentLinks.Count > 0)
            {
                ILink childLink = Link.FromSpanContext(span.Context, LinkType.CHILD_LINKED_SPAN);
                foreach (ISpan linkedSpan in parentLinks)
                {
                    linkedSpan.AddLink(childLink);
                    span.AddLink(Link.FromSpanContext(linkedSpan.Context, LinkType.PARENT_LINKED_SPAN));
                }
            }
        }

        private static bool MakeSamplingDecision(
            ISpanContext parent,
            bool hasRemoteParent,
            string name,
            ISampler sampler,
            IList<ISpan> parentLinks,
            ITraceId traceId,
            ISpanId spanId,
            ITraceParams activeTraceParams)
        {
            // If users set a specific sampler in the SpanBuilder, use it.
            if (sampler != null)
            {
                return sampler.ShouldSample(parent, hasRemoteParent, traceId, spanId, name, parentLinks);
            }

            // Use the default sampler if this is a root Span or this is an entry point Span (has remote
            // parent).
            if (hasRemoteParent || parent == null || !parent.IsValid)
            {
                return activeTraceParams
                    .Sampler
                    .ShouldSample(parent, hasRemoteParent, traceId, spanId, name, parentLinks);
            }

            // Parent is always different than null because otherwise we use the default sampler.
            return parent.TraceOptions.IsSampled || IsAnyParentLinkSampled(parentLinks);
        }
    }
}

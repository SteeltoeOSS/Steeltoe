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

using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    internal class SpanExporterWorker : IDisposable
    {
        private readonly int _bufferSize;
        private readonly TimeSpan _scheduleDelay;
        private bool _shutdown = false;
        private readonly BlockingCollection<ISpan> _spans;
        private readonly ConcurrentDictionary<string, IHandler> _serviceHandlers = new ConcurrentDictionary<string, IHandler>();

        public SpanExporterWorker(int bufferSize, IDuration scheduleDelay)
        {
            _bufferSize = bufferSize;
            _scheduleDelay = TimeSpan.FromSeconds(scheduleDelay.Seconds);
            _spans = new BlockingCollection<ISpan>();
        }

        public void Dispose()
        {
            _shutdown = true;
            _spans.CompleteAdding();
        }

        internal void AddSpan(ISpan span)
        {
            if (!_spans.IsAddingCompleted)
            {
                if (!_spans.TryAdd(span))
                {
                    // Log failure, dropped span
                }
            }
        }

        internal void Run(object obj)
        {
            List<ISpanData> toExport = new List<ISpanData>();
            while (!_shutdown)
            {
                try
                {
                    if (_spans.TryTake(out ISpan item, _scheduleDelay))
                    {
                        // Build up list
                        BuildList(item, toExport);

                        // Export them
                        Export(toExport);

                        // Get ready for next batch
                        toExport.Clear();
                    }

                    if (_spans.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    // Log
                    return;
                }
            }
        }

        private void BuildList(ISpan item, List<ISpanData> toExport)
        {
            Span span = item as Span;
            if (span != null)
            {
                toExport.Add(span.ToSpanData());
            }

            // Grab as many as we can
            while (_spans.TryTake(out item))
            {
                span = item as Span;
                if (span != null)
                {
                    toExport.Add(span.ToSpanData());
                }

                if (toExport.Count >= _bufferSize)
                {
                    break;
                }
            }
        }

        private void Export(IList<ISpanData> export)
        {
            var handlers = _serviceHandlers.Values;
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Export(export);
                }
                catch (Exception)
                {
                    // Log warning
                }
            }
        }

        internal void RegisterHandler(string name, IHandler handler)
        {
            _serviceHandlers[name] = handler;
        }

        internal void UnregisterHandler(string name)
        {
            _serviceHandlers.TryRemove(name, out IHandler prev);
        }

        internal ISpanData ToSpanData(ISpan span)
        {
            Span spanImpl = span as Span;
            if (spanImpl == null)
            {
                throw new InvalidOperationException("ISpan not a Span");
            }

            return spanImpl.ToSpanData();
        }
    }
}

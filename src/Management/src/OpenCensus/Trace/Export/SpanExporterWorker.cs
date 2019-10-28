// <copyright file="SpanExporterWorker.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace.Export
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenCensus.Common;
    using OpenCensus.Implementation;

    internal class SpanExporterWorker : IDisposable
    {
        private readonly int bufferSize;
        private readonly BlockingCollection<ISpan> spans;
        private readonly ConcurrentDictionary<string, IHandler> serviceHandlers = new ConcurrentDictionary<string, IHandler>();
        private readonly TimeSpan scheduleDelay;
        private bool shutdown = false;

        public SpanExporterWorker(int bufferSize, IDuration scheduleDelay)
        {
            this.bufferSize = bufferSize;
            this.scheduleDelay = TimeSpan.FromSeconds(scheduleDelay.Seconds);
            this.spans = new BlockingCollection<ISpan>();
        }

        public void Dispose()
        {
            this.shutdown = true;
            this.spans.CompleteAdding();
        }

        internal void AddSpan(ISpan span)
        {
            if (!this.spans.IsAddingCompleted)
            {
                if (!this.spans.TryAdd(span))
                {
                    // Log failure, dropped span
                }
            }
        }

        internal Task ExportAsync(IEnumerable<ISpanData> export, CancellationToken token)
        {
            var handlers = this.serviceHandlers.Values;
            foreach (var handler in handlers)
            {
                try
                {
                    // TODO: when handlers interface will be switched to async - this need to await
                    handler.Export(export);
                }
                catch (Exception ex)
                {
                    OpenCensusEventSource.Log.ExporterThrownExceptionWarning(ex);
                }
            }

            return Task.CompletedTask;
        }

        internal void Run(object obj)
        {
            List<ISpanData> toExport = new List<ISpanData>();
            while (!this.shutdown)
            {
                try
                {
                    if (this.spans.TryTake(out ISpan item, this.scheduleDelay))
                    {
                        // Build up list
                        this.BuildList(item, toExport);

                        // Export them
                        this.ExportAsync(toExport, CancellationToken.None);

                        // Get ready for next batch
                        toExport.Clear();
                    }

                    if (this.spans.IsCompleted)
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

        internal void RegisterHandler(string name, IHandler handler)
        {
            this.serviceHandlers[name] = handler;
        }

        internal void UnregisterHandler(string name)
        {
            this.serviceHandlers.TryRemove(name, out IHandler prev);
        }

        internal ISpanData ToSpanData(ISpan span)
        {
            if (!(span is Span spanImpl))
            {
                throw new InvalidOperationException("ISpan not a Span");
            }

            return spanImpl.ToSpanData();
        }

        private void BuildList(ISpan item, List<ISpanData> toExport)
        {
            if (item is Span span)
            {
                toExport.Add(span.ToSpanData());
            }

            // Grab as many as we can
            while (this.spans.TryTake(out item))
            {
                span = item as Span;
                if (span != null)
                {
                    toExport.Add(span.ToSpanData());
                }

                if (toExport.Count >= this.bufferSize)
                {
                    break;
                }
            }
        }
    }
}

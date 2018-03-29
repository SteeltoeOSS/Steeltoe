using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Common;

namespace Steeltoe.Management.Census.Trace.Export
{
    internal class SpanExporterWorker : IDisposable
    {
        private int _bufferSize;
        private TimeSpan _scheduleDelay;
        private bool _shutdown = false;
        private BlockingCollection<ISpan> _spans;
        private ConcurrentDictionary<string, IHandler> _serviceHandlers = new ConcurrentDictionary<string, IHandler>();

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
            while(!_shutdown)
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
                } catch(Exception)
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

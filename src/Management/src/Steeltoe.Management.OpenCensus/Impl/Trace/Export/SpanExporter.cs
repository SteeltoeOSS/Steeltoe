using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SpanExporter : SpanExporterBase
    {
        private SpanExporterWorker _worker { get; }
        private readonly Thread _workerThread;

        internal SpanExporter(SpanExporterWorker worker)
        {
            _worker = worker;
            _workerThread = new Thread(worker.Run)
            {
                IsBackground = true,
                Name = "SpanExporter"
            };
            _workerThread.Start();
        }

        internal static ISpanExporter Create(int bufferSize, IDuration scheduleDelay)
        {
            SpanExporterWorker worker = new SpanExporterWorker(bufferSize, scheduleDelay);
            return new SpanExporter(worker);
        }

        public override void AddSpan(ISpan span)
        {
            _worker.AddSpan(span);
        }

        public override void RegisterHandler(string name, IHandler handler)
        {
            _worker.RegisterHandler(name, handler);
        }

        public override void UnregisterHandler(string name)
        {
            _worker.UnregisterHandler(name);
        }

        public override void Dispose()
        {
            _worker.Dispose();
        }

        internal Thread ServiceExporterThread
        {
            get
            {
                return _workerThread;
            }
        }
    }
}

using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class SpanExporterBase : ISpanExporter
    {
        private static readonly ISpanExporter NOOP_SPAN_EXPORTER = new NoopSpanExporter();
        public static ISpanExporter NoopSpanExporter
        {
            get
            {
                return NOOP_SPAN_EXPORTER;
            }
        }

        public abstract void AddSpan(ISpan span);
        public abstract void Dispose();
        public abstract void RegisterHandler(string name, IHandler handler);
        public abstract void UnregisterHandler(string name);

    }
}

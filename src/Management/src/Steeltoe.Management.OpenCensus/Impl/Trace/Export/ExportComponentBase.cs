using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class ExportComponentBase : IExportComponent
    {
        public  static IExportComponent NewNoopExportComponent
        {
            get
            {
                return new NoopExportComponent();
            }
        }
        public abstract ISpanExporter SpanExporter { get; }

        public abstract IRunningSpanStore RunningSpanStore { get; }

        public abstract ISampledSpanStore SampledSpanStore { get; }

    }
}

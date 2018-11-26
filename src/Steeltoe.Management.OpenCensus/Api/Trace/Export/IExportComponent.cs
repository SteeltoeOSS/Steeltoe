using System;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IExportComponent
    {
        ISpanExporter SpanExporter { get; }
        IRunningSpanStore RunningSpanStore { get; }
        ISampledSpanStore SampledSpanStore { get; }
    }
}
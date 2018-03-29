namespace Steeltoe.Management.Census.Trace.Config
{
    public interface ITraceConfig
    {
        ITraceParams ActiveTraceParams { get; }
        void UpdateActiveTraceParams(ITraceParams traceParams);
    }
}
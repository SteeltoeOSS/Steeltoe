
namespace Steeltoe.Management.Census.Trace.Config
{
    public interface ITraceParams
    {
        ISampler Sampler { get; }
        int MaxNumberOfAttributes { get; }
        int MaxNumberOfAnnotations { get; }
        int MaxNumberOfMessageEvents { get; }
        int MaxNumberOfLinks { get; }
        TraceParamsBuilder ToBuilder();
    }
}

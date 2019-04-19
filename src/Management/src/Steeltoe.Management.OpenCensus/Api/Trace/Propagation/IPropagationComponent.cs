using System;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IPropagationComponent
    {
        IBinaryFormat BinaryFormat { get; }
        ITextFormat TextFormat { get; }
    }
}
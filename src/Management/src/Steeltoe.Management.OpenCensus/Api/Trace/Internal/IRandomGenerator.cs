

using System;

namespace Steeltoe.Management.Census.Trace.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IRandomGenerator
    {
        void NextBytes(byte[] bytes);
    }
}

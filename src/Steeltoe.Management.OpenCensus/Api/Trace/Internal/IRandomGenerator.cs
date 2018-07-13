

namespace Steeltoe.Management.Census.Trace.Internal
{
    public interface IRandomGenerator
    {
        void NextBytes(byte[] bytes);
    }
}

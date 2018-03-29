

using Steeltoe.Management.Census.Common;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface ITimedEvent<T>
    {
        ITimestamp Timestamp { get; }
        T Event { get; }
    }
}

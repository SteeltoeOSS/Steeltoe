using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITagPropagationComponent
    {
        ITagContextBinarySerializer BinarySerializer { get; }
    }
}

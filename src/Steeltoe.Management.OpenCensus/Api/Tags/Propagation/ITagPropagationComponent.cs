using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    public interface ITagPropagationComponent
    {
        ITagContextBinarySerializer BinarySerializer { get; }
    }
}

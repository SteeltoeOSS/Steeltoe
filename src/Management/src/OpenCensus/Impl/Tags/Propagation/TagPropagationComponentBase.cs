using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TagPropagationComponentBase : ITagPropagationComponent
    {
        protected TagPropagationComponentBase() { }
        public abstract ITagContextBinarySerializer BinarySerializer { get; }
    }
}

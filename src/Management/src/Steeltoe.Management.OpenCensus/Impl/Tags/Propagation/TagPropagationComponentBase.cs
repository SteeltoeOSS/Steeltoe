using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    public abstract class TagPropagationComponentBase : ITagPropagationComponent
    {
        protected TagPropagationComponentBase() { }
        public abstract ITagContextBinarySerializer BinarySerializer { get; }
    }
}

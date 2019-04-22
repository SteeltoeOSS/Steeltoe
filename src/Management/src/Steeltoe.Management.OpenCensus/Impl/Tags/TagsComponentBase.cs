using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Tags.Propagation;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TagsComponentBase : ITagsComponent
    {
        public abstract ITagger Tagger { get; }
        public abstract ITagPropagationComponent TagPropagationComponent { get; }
        public abstract TaggingState State { get; }
    }
}

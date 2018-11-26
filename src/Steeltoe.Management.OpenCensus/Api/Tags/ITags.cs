
using Steeltoe.Management.Census.Tags.Propagation;
using System;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITags
    {
        ITagger Tagger { get; }

        ITagPropagationComponent TagPropagationComponent { get; }
  
        TaggingState State { get; }
    }
}

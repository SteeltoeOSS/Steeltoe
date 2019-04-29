using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITagsComponent
    {

        ITagger Tagger { get; }

        ITagPropagationComponent TagPropagationComponent { get; }

        TaggingState State { get; }
    }
}
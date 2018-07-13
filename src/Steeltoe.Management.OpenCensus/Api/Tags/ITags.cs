
using Steeltoe.Management.Census.Tags.Propagation;

namespace Steeltoe.Management.Census.Tags
{
    public interface ITags
    {
        ITagger Tagger { get; }

        ITagPropagationComponent TagPropagationComponent { get; }
  
        TaggingState State { get; }
    }
}

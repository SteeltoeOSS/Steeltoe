using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public class TagsComponent : TagsComponentBase
    {
        // The TaggingState shared between the TagsComponent, Tagger, and TagPropagationComponent
        private readonly CurrentTaggingState state;
        private readonly ITagger tagger;
        private readonly ITagPropagationComponent tagPropagationComponent;

        public TagsComponent()
        {
            state = new CurrentTaggingState();
            tagger = new Tagger(state);
            tagPropagationComponent = new TagPropagationComponent(state);
        }
        public override ITagger Tagger
        {
            get { return tagger; }
        }

        public override ITagPropagationComponent TagPropagationComponent
        {
            get { return tagPropagationComponent; }
        }

        public override TaggingState State
        {
            get { return state.Value; }
        }
    }
}

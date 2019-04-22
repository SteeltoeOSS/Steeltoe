using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public sealed class NoopTagsComponent : TagsComponentBase
    {
        //private volatile bool isRead;


        public override ITagger Tagger
        {
            get
            {
                return NoopTags.NoopTagger;
            }
        }


        public override ITagPropagationComponent TagPropagationComponent
        {
            get
            {
                return NoopTags.NoopTagPropagationComponent;
            }
        }


        public override TaggingState State
        {
            get
            {
                //isRead = true;
                return TaggingState.DISABLED;
            }
        }
    }
}

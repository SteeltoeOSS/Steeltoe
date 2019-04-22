using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public class NoopTagPropagationComponent : TagPropagationComponentBase
    {
        internal static readonly ITagPropagationComponent INSTANCE = new NoopTagPropagationComponent();

        public override ITagContextBinarySerializer BinarySerializer
        {
            get
            {
                return NoopTags.NoopTagContextBinarySerializer;
            }
        }
    }
}

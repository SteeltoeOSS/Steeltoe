using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopTags
    {

        internal static ITagsComponent NewNoopTagsComponent()
        {
            return new NoopTagsComponent();
        }

        internal static ITagger NoopTagger
        {
            get
            {
                return Census.Tags.NoopTagger.INSTANCE;
            }
        }

        internal static ITagContextBuilder NoopTagContextBuilder
        {
            get
            {
                return Census.Tags.NoopTagContextBuilder.INSTANCE;
            }
        }

        internal static ITagContext NoopTagContext
        {
            get
            {
                return Census.Tags.NoopTagContext.INSTANCE;
            }
        }

        internal static ITagPropagationComponent NoopTagPropagationComponent
        {
            get
            {
                return Census.Tags.NoopTagPropagationComponent.INSTANCE;
            }
        }

        internal static ITagContextBinarySerializer NoopTagContextBinarySerializer
        {
            get
            {
                return Census.Tags.NoopTagContextBinarySerializer.INSTANCE;
            }
        }
    }
}

using Steeltoe.Management.Census.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Tags
    {
        private static object _lck = new object();
        internal static void Initialize(bool enabled)
        {
            if (_tags == null)
            {
                lock (_lck)
                {
                    _tags =_tags ?? new Tags(enabled);
                }
            }
        }

        private static Tags _tags;

        internal Tags()
            : this(false)
        {

        }
        internal Tags(bool enabled)
        {
            if (enabled)
            {
                tagsComponent = new TagsComponent();
            }
            else
            {
                tagsComponent = NoopTags.NewNoopTagsComponent();
            }
        }

        private readonly ITagsComponent tagsComponent = new TagsComponent();


        public static ITagger Tagger
        {
            get
            {
                Initialize(false);
                return _tags.tagsComponent.Tagger;
            }
        }
        public static ITagPropagationComponent TagPropagationComponent
        {
            get
            {
                Initialize(false);
                return _tags.tagsComponent.TagPropagationComponent;
            }
        }
        public static TaggingState State
        {
            get
            {
                Initialize(false);
                return _tags.tagsComponent.State;
            }
        }
    }
}

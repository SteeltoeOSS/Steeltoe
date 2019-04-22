using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    internal sealed class TagContextBuilder : TagContextBuilderBase
    {
        internal IDictionary<ITagKey, ITagValue> Tags { get; }

        internal TagContextBuilder(IDictionary<ITagKey, ITagValue> tags)
        {
            this.Tags = new Dictionary<ITagKey, ITagValue>(tags);
        }

        internal TagContextBuilder()
        {
            this.Tags = new Dictionary<ITagKey, ITagValue>();
        }

        public override ITagContextBuilder Put(ITagKey key, ITagValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            Tags[key] = value;
            return this;
        }

        public override ITagContextBuilder Remove(ITagKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (Tags.ContainsKey(key))
            {
                Tags.Remove(key);
            }
            return this;
        }

        public override ITagContext Build()
        {
            return new TagContext(Tags);
        }

        public override IScope BuildScoped()
        {
            return CurrentTagContextUtils.WithTagContext(Build());
        }
    }
}

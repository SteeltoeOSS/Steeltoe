using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public sealed class Tag : ITag
    {
        public ITagKey Key { get; }
        public ITagValue Value { get; }
        internal Tag(ITagKey key, ITagValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            this.Key = key;
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            this.Value = value;
        }
        public static ITag Create(ITagKey key, ITagValue value)
        {
            return new Tag(key, value);
        }
        public override string ToString()
        {
            return "Tag{"
                + "key=" + Key + ", "
                + "value=" + Value
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Tag)
            {
                Tag that = (Tag)o;
                return (this.Key.Equals(that.Key))
                     && (this.Value.Equals(that.Value));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Key.GetHashCode();
            h *= 1000003;
            h ^= this.Value.GetHashCode();
            return h;
        }
    }
}

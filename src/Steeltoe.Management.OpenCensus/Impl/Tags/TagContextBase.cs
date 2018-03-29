using Steeltoe.Management.Census.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Tags
{
    public abstract class TagContextBase : ITagContext
    {

        public override string ToString()
        {
            return "TagContext";
        }

        public override bool Equals(Object other)
        {
            if (!(other is TagContextBase)) {
                return false;
            }
            TagContextBase otherTags = (TagContextBase)other;

            var t1Enumerator = this.GetEnumerator();
            var t2Enumerator = otherTags.GetEnumerator();

            List<ITag> tags1 = null;
            List<ITag> tags2 = null;

            if (t1Enumerator == null)
            {
                tags1 = new List<ITag>();
            } else
            {
                tags1 = this.ToList();
            }
            if (t2Enumerator == null)
            {
                tags2 = new List<ITag>();
            }
            else
            {
                tags2 = otherTags.ToList();
            }
            return Collections.AreEquivalent(tags1, tags2);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (var t in this)
            {
                hashCode += t.GetHashCode();
            }

            return hashCode;
        }

        public abstract IEnumerator<ITag> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}

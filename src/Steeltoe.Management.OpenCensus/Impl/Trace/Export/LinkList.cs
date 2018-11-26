using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class LinkList : ILinks
    {
        public static LinkList Create(IList<ILink> links, int droppedLinksCount)
        {
            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }
            List<ILink> copy = new List<ILink>(links);

            return new LinkList(copy.AsReadOnly(), droppedLinksCount);
        }
        internal LinkList(IList<ILink> links, int droppedLinksCount)
        {
            if (links == null)
            {
                throw new ArgumentNullException("Null links");
            }
            this.Links = links;
            this.DroppedLinksCount = droppedLinksCount;
        }

        public int DroppedLinksCount { get; }

        public IList<ILink> Links { get; }

        public override string ToString()
        {
            return "Links{"
                + "links=" + Links + ", "
                + "droppedLinksCount=" + DroppedLinksCount
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is LinkList)
            {
                LinkList that = (LinkList)o;
                return (this.Links.SequenceEqual(that.Links))
                     && (this.DroppedLinksCount == that.DroppedLinksCount);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Links.GetHashCode();
            h *= 1000003;
            h ^= this.DroppedLinksCount;
            return h;
        }
    }
}

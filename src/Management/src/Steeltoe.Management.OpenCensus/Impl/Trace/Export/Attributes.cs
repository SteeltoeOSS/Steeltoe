using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class Attributes : IAttributes
    {
        public IDictionary<string, IAttributeValue> AttributeMap { get; }

        public int DroppedAttributesCount { get; }

        public static Attributes Create(IDictionary<string, IAttributeValue> attributeMap, int droppedAttributesCount)
        {
            if (attributeMap == null)
            {
                throw new ArgumentNullException(nameof(attributeMap));
            }
            IDictionary<string, IAttributeValue> copy = new Dictionary<string, IAttributeValue>(attributeMap);
            return new Attributes(new ReadOnlyDictionary<string, IAttributeValue>(copy), droppedAttributesCount);
        }

        internal Attributes(IDictionary<string, IAttributeValue> attributeMap, int droppedAttributesCount)
        {
            if (attributeMap == null)
            {
                throw new ArgumentNullException("Null attributeMap");
            }
            AttributeMap = attributeMap;
            DroppedAttributesCount = droppedAttributesCount;
        }
        public override string ToString()
        {
            return "Attributes{"
                + "attributeMap=" + AttributeMap + ", "
                + "droppedAttributesCount=" + DroppedAttributesCount
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Attributes)
            {
                Attributes that = (Attributes)o;
                return (this.AttributeMap.SequenceEqual(that.AttributeMap))
                     && (this.DroppedAttributesCount == that.DroppedAttributesCount);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.AttributeMap.GetHashCode();
            h *= 1000003;
            h ^= this.DroppedAttributesCount;
            return h;
        }
    }
}

using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public sealed class TagValues
    {
        private TagValues(IList<ITagValue> values)
        {
            Values = values;
        }

        public IList<ITagValue> Values { get; }
        public static TagValues Create(IList<ITagValue> values)
        {
            return new TagValues(values);
        }

        public override string ToString()
        {
            return "TagValues{"
                + "values=" + Collections.ToString(Values)
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is TagValues)
            {
                TagValues that = (TagValues)o;
                if (Values.Count != that.Values.Count)
                {
                    return false;
                }

                for(int i = 0; i < Values.Count; i++)
                {
                    if (Values[i] == null)
                    {
                        if (that.Values[i] != null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Values[i].Equals(that.Values[i]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            foreach(var v in Values)
            {
                if (v != null)
                {
                    h ^= v.GetHashCode();
                }
            }
            return h;
        }
    }
}

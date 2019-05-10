// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
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

        public override bool Equals(object o)
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

                for (int i = 0; i < Values.Count; i++)
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
            foreach (var v in Values)
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

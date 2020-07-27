// <copyright file="TagValues.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Tags
{
    using System.Collections.Generic;
    using OpenCensus.Abstractions.Utils;
    using OpenCensus.Utils;

    /// <summary>
    /// Collection of tags.
    /// </summary>
    public sealed class TagValues
    {
        private TagValues(IList<ITagValue> values)
        {
            Values = values;
        }

        /// <summary>
        /// Gets the collection of tag values.
        /// </summary>
        public IList<ITagValue> Values { get; }

        /// <summary>
        /// Create tag values out of list of tags.
        /// </summary>
        /// <param name="values">Values to create tag values from.</param>
        /// <returns>Resulting tag values collection.</returns>
        public static TagValues Create(IList<ITagValue> values)
        {
            return new TagValues(values);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "TagValues{"
                + "values=" + Collections.ToString(Values)
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TagValues that)
            {
                if (Values.Count != that.Values.Count)
                {
                    return false;
                }

                for (var i = 0; i < Values.Count; i++)
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

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
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

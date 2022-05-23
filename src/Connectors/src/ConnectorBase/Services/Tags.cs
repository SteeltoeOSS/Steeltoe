// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Connector.Services
{
    public class Tags
    {
        public Tags(string tag)
            : this(new string[] { tag })
        {
        }

        public Tags(string[] tags)
        {
            Values = tags ?? System.Array.Empty<string>();
        }

        internal Tags()
        {
        }

        public IEnumerable<string> Values { get; internal protected set; }

        public bool ContainsOne(IEnumerable<string> tags)
        {
            return tags != null && Values != null && tags.Intersect(Values).Any();
        }

        public bool Contains(string tag)
        {
            if (Values == null)
            {
                return false;
            }

            return Values.Contains(tag);
        }

        public bool StartsWith(string tag)
        {
            if (tag != null && Values != null)
            {
                foreach (var t in Values)
                {
                    if (tag.StartsWith(t))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

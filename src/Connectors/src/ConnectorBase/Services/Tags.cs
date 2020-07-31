// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class Tags
    {
        public Tags(string tag)
            : this(new string[] { tag })
        {
        }

        public Tags(string[] tags)
        {
            if (tags == null)
            {
                Values = System.Array.Empty<string>();
            }
            else
            {
                Values = tags;
            }
        }

        internal Tags()
        {
        }

        public string[] Values { get; internal protected set; }

        public bool ContainsOne(string[] tags)
        {
            if (tags != null && Values != null)
            {
                foreach (var value in Values)
                {
                    if (tags.Contains(value))
                    {
                        return true;
                    }
                }
            }

            return false;
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

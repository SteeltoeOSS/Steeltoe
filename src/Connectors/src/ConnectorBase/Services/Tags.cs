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

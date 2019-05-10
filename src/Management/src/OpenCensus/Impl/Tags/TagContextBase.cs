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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TagContextBase : ITagContext
    {
        public override string ToString()
        {
            return "TagContext";
        }

        public override bool Equals(object other)
        {
            if (!(other is TagContextBase))
            {
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
            }
            else
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

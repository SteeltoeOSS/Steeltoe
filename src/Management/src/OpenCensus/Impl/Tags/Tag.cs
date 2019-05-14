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

using System;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Tag : ITag
    {
        public ITagKey Key { get; }

        public ITagValue Value { get; }

        internal Tag(ITagKey key, ITagValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.Key = key;
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.Value = value;
        }

        public static ITag Create(ITagKey key, ITagValue value)
        {
            return new Tag(key, value);
        }

        public override string ToString()
        {
            return "Tag{"
                + "key=" + Key + ", "
                + "value=" + Value
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is Tag)
            {
                Tag that = (Tag)o;
                return this.Key.Equals(that.Key)
                     && this.Value.Equals(that.Value);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Key.GetHashCode();
            h *= 1000003;
            h ^= this.Value.GetHashCode();
            return h;
        }
    }
}

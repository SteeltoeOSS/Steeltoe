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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TagContext : TagContextBase
    {
        public static readonly ITagContext EMPTY = new TagContext(new Dictionary<ITagKey, ITagValue>());

        public IDictionary<ITagKey, ITagValue> Tags { get; }

        public TagContext(IDictionary<ITagKey, ITagValue> tags)
        {
            this.Tags = new ReadOnlyDictionary<ITagKey, ITagValue>(new Dictionary<ITagKey, ITagValue>(tags));
        }

        public override IEnumerator<ITag> GetEnumerator()
        {
            var result = Tags.Select((kvp) => Tag.Create(kvp.Key, kvp.Value));
            return result.ToList().GetEnumerator();
        }
    }
}

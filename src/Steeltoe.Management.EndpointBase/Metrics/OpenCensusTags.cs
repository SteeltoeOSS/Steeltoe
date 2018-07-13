// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Census.Tags.Propagation;
using System;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class OpenCensusTags : ITags
    {
        private static readonly Lazy<OpenCensusTags> AsSingleton = new Lazy<OpenCensusTags>(() => new OpenCensusTags());

        public static OpenCensusTags Instance => AsSingleton.Value;

        private readonly ITagsComponent tagsComponent = new TagsComponent();

        public ITagger Tagger
        {
            get
            {
                return tagsComponent.Tagger;
            }
        }

        public ITagPropagationComponent TagPropagationComponent
        {
            get
            {
                return tagsComponent.TagPropagationComponent;
            }
        }

        public TaggingState State
        {
            get
            {
                return tagsComponent.State;
            }
        }
    }
}

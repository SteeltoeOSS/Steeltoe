// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Tags;
using OpenCensus.Tags.Propagation;
using System;

namespace Steeltoe.Management.Census.Tags
{
    public class OpenCensusTags : ITags
    {
        private static readonly Lazy<OpenCensusTags> AsSingleton = new Lazy<OpenCensusTags>(() => new OpenCensusTags());

        public static OpenCensusTags Instance => AsSingleton.Value;

        private readonly ITagsComponent _tagsComponent = new TagsComponent();

        public ITagger Tagger
        {
            get
            {
                return _tagsComponent.Tagger;
            }
        }

        public ITagPropagationComponent TagPropagationComponent
        {
            get
            {
                return _tagsComponent.TagPropagationComponent;
            }
        }

        public TaggingState State
        {
            get
            {
                return _tagsComponent.State;
            }
        }
    }
}

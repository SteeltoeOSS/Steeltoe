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

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class TagContextBinarySerializer : TagContextBinarySerializerBase
    {
        private static readonly byte[] EMPTY_BYTE_ARRAY = { };

        private readonly CurrentTaggingState state;

        internal TagContextBinarySerializer(CurrentTaggingState state)
        {
            this.state = state;
        }

        public override byte[] ToByteArray(ITagContext tags)
        {
            return state.Internal == TaggingState.DISABLED
                ? EMPTY_BYTE_ARRAY
                : SerializationUtils.SerializeBinary(tags);
        }

        public override ITagContext FromByteArray(byte[] bytes)
        {
            return state.Internal == TaggingState.DISABLED
                ? TagContext.EMPTY
                : SerializationUtils.DeserializeBinary(bytes);
        }
    }
}

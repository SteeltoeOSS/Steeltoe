using System;
using System.Collections.Generic;
using System.Text;

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

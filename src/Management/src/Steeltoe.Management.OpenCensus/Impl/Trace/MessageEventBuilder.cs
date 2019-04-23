using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class MessageEventBuilder
    {
        private MessageEventType? type;
        private Int64? messageId;
        private Int64? uncompressedMessageSize;
        private Int64? compressedMessageSize;

        internal MessageEventBuilder()
        {
        }
        internal MessageEventBuilder(
            MessageEventType type,
            long messageId,
            long uncompressedMessageSize,
            long compressedMessageSize)
        {
            this.type = type;
            this.messageId = messageId;
            this.uncompressedMessageSize = uncompressedMessageSize;
            this.compressedMessageSize = compressedMessageSize;
        }
        internal MessageEventBuilder SetType(MessageEventType type)
        {
            this.type = type;
            return this;
        }
        internal MessageEventBuilder SetMessageId(long messageId)
        {
            this.messageId = messageId;
            return this;
        }
        public MessageEventBuilder SetUncompressedMessageSize(long uncompressedMessageSize)
        {
            this.uncompressedMessageSize = uncompressedMessageSize;
            return this;
        }
        public MessageEventBuilder SetCompressedMessageSize(long compressedMessageSize)
        {
            this.compressedMessageSize = compressedMessageSize;
            return this;
        }

        public IMessageEvent Build()
        {
            string missing = "";
            if (!type.HasValue)
            {
                missing += " type";
            }
            if (!this.messageId.HasValue)
            {
                missing += " messageId";
            }
            if (!this.uncompressedMessageSize.HasValue)
            {
                missing += " uncompressedMessageSize";
            }
            if (!this.compressedMessageSize.HasValue)
            {
                missing += " compressedMessageSize";
            }
            if (!string.IsNullOrEmpty(missing))
            {
                throw new ArgumentOutOfRangeException("Missing required properties:" + missing);
            }
            return new MessageEvent(
                this.type.Value,
                this.messageId.Value,
                this.uncompressedMessageSize.Value,
                this.compressedMessageSize.Value);
        }
    }
}

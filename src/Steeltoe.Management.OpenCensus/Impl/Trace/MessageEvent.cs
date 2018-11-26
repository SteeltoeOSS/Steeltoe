using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class MessageEvent : IMessageEvent
    {
        internal MessageEvent(MessageEventType type, long messageId, long uncompressedMessageSize, long compressedMessageSize)
        {
            Type = type;
            MessageId = messageId;
            UncompressedMessageSize = uncompressedMessageSize;
            CompressedMessageSize = compressedMessageSize;
        }

        public MessageEventType Type { get; }

        public long MessageId { get; }

        public long UncompressedMessageSize { get; }

        public long CompressedMessageSize { get; }

        public static MessageEventBuilder Builder(MessageEventType type, long messageId)
        {
            return new MessageEventBuilder()
                    .SetType(type)
                    .SetMessageId(messageId)
                    // We need to set a value for the message size because the autovalue requires all
                    // primitives to be initialized.
                    .SetUncompressedMessageSize(0)
                    .SetCompressedMessageSize(0);
        }

        public override string ToString()
        {
            return "MessageEvent{"
                + "type=" + Type + ", "
                + "messageId=" + MessageId + ", "
                + "uncompressedMessageSize=" + UncompressedMessageSize + ", "
                + "compressedMessageSize=" + CompressedMessageSize
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is MessageEvent)
            {
                MessageEvent that = (MessageEvent)o;
                return (this.Type.Equals(that.Type))
                     && (this.MessageId == that.MessageId)
                     && (this.UncompressedMessageSize == that.UncompressedMessageSize)
                     && (this.CompressedMessageSize == that.CompressedMessageSize);
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= this.Type.GetHashCode();
            h *= 1000003;
            h ^= (this.MessageId >> 32) ^ this.MessageId;
            h *= 1000003;
            h ^= (this.UncompressedMessageSize >> 32) ^ this.UncompressedMessageSize;
            h *= 1000003;
            h ^= (this.CompressedMessageSize >> 32) ^ this.CompressedMessageSize;
            return (int)h;
        }
    }
}

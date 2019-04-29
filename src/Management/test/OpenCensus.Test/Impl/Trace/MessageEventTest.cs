using System;

using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class MessageEventTest
    {

        [Fact]
        public void BuildMessageEvent_WithRequiredFields()
        {
            IMessageEvent messageEvent = MessageEvent.Builder(MessageEventType.SENT, 1L).Build();
            Assert.Equal(MessageEventType.SENT, messageEvent.Type);
            Assert.Equal(1L, messageEvent.MessageId);
            Assert.Equal(0L, messageEvent.UncompressedMessageSize);
        }

        [Fact]
        public void BuildMessageEvent_WithUncompressedMessageSize()
        {
            IMessageEvent messageEvent =
                MessageEvent.Builder(MessageEventType.SENT, 1L).SetUncompressedMessageSize(123L).Build();
            Assert.Equal(MessageEventType.SENT, messageEvent.Type);
            Assert.Equal(1L, messageEvent.MessageId);
            Assert.Equal(123L, messageEvent.UncompressedMessageSize);
        }

        [Fact]
        public void BuildMessageEvent_WithCompressedMessageSize()
        {
            IMessageEvent messageEvent =
                MessageEvent.Builder(MessageEventType.SENT, 1L).SetCompressedMessageSize(123L).Build();
            Assert.Equal(MessageEventType.SENT, messageEvent.Type);
            Assert.Equal(1L, messageEvent.MessageId);
            Assert.Equal(123L, messageEvent.CompressedMessageSize);
        }

        [Fact]
        public void BuildMessageEvent_WithAllValues()
        {
            IMessageEvent messageEvent =
                MessageEvent.Builder(MessageEventType.RECEIVED, 1L)
                    .SetUncompressedMessageSize(123L)
                    .SetCompressedMessageSize(63L)
                    .Build();
            Assert.Equal(MessageEventType.RECEIVED, messageEvent.Type);
            Assert.Equal(1L, messageEvent.MessageId);
            Assert.Equal(123L, messageEvent.UncompressedMessageSize);
            Assert.Equal(63L, messageEvent.CompressedMessageSize);
        }

        [Fact]
        public void MessageEvent_ToString()
        {
            IMessageEvent messageEvent =
                MessageEvent.Builder(MessageEventType.SENT, 1L)
                    .SetUncompressedMessageSize(123L)
                    .SetCompressedMessageSize(63L)
                    .Build();
            Assert.Contains("type=SENT", messageEvent.ToString());
            Assert.Contains("messageId=1", messageEvent.ToString());
            Assert.Contains("compressedMessageSize=63", messageEvent.ToString());
            Assert.Contains("uncompressedMessageSize=123", messageEvent.ToString());
        }
    }
}


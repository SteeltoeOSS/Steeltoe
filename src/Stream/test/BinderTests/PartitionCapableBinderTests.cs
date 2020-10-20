using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder
{
    public abstract class PartitionCapableBinderTests<B, T> : AbstractBinderTests<B, T>
        //where B : AbstractBinder<IMessageChannel>
        where B : AbstractTestBinder<T>
        where T : AbstractBinder<IMessageChannel>
    {
        //protected static SpelExpressionParser
        protected PartitionCapableBinderTests(ITestOutputHelper output)
            : base(output)
        {

            //oducerOptions.PartitionKeyExpression = ??
        }

        [Fact]
        public void testAnonymousGroup()
        {
            B binder = GetBinder();
            var producerBindingOptions = CreateProducerBindingOptions(CreateProducerOptions());
            var output = CreateBindableChannel("output", producerBindingOptions);

            var producerBinding = binder.BindProducer(string.Format("defaultGroup%s0", GetDestinationNameDelimiter()), output, producerBindingOptions.Producer);

            QueueChannel input1 = new QueueChannel();
            var binding1 = binder.BindConsumer(string.Format("defaultGroup%s0", GetDestinationNameDelimiter()), null, input1, CreateConsumerOptions());

            QueueChannel input2 = new QueueChannel();
            var binding2 = binder.BindConsumer(string.Format("defaultGroup%s0", GetDestinationNameDelimiter()), null, input2, CreateConsumerOptions());

            var testPayload1 = "foo-" + Guid.NewGuid().ToString();
            output.Send(MessageBuilder.WithPayload(testPayload1)
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                    .Build());

            Message<byte[]> receivedMessage1 = (Message<byte[]>)Receive(input1);
            Assert.NotNull(receivedMessage1);
            Assert.Equal(receivedMessage1.Payload.ToString(), testPayload1);

            Message<byte[]> receivedMessage2 = (Message<byte[]>)Receive(input2);
            Assert.NotNull(receivedMessage2);
            Assert.Equal(receivedMessage2.Payload.ToString(), testPayload1);

            binding2.Unbind();

            var testPayload2 = "foo-" + Guid.NewGuid().ToString();

            output.Send(MessageBuilder.WithPayload(testPayload2)
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                    .Build());

            binding2 = binder.BindConsumer(
                    string.Format("defaultGroup%s0", GetDestinationNameDelimiter()), null,
                    input2, CreateConsumerOptions());
            var testPayload3 = "foo-" + Guid.NewGuid().ToString();
            output.Send(MessageBuilder.WithPayload(testPayload3)
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                    .Build());

            receivedMessage1 = (Message<byte[]>)Receive(input1);
            Assert.NotNull(receivedMessage1);
            Assert.Equal(receivedMessage1.Payload.ToString(), testPayload2);
            receivedMessage1 = (Message<byte[]>)Receive(input1);
            Assert.NotNull(receivedMessage1);
            Assert.NotNull(receivedMessage1.Payload);

            receivedMessage2 = (Message<byte[]>)Receive(input2);
            Assert.NotNull(receivedMessage2);
            Assert.Equal(receivedMessage2.Payload.ToString(), testPayload3);

            producerBinding.Unbind();
            binding1.Unbind();
            binding2.Unbind();
        }

        protected RabbitInboundChannelAdapter ExtractEndpoint(IBinding binding)
        {
            return GetFieldValue<RabbitInboundChannelAdapter>(binding, "_lifecycle");
        }

        protected T GetFieldValue<T>(object current, string name)
        {
            var fi = current.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)fi.GetValue(current);
        }

        protected T GetPropertyValue<T>(object current, string name)
        {
            var pi = current.GetType().GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)pi.GetValue(current);
        }
    }
}

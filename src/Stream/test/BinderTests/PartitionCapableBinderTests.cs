using Microsoft.Extensions.Logging;
using Steeltoe.Common.Expression.CSharp;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
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
        protected static ExpressionParser expressionParser = new ExpressionParser();

        protected PartitionCapableBinderTests(ITestOutputHelper output, ILogger logger)
            : base(output, logger)
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
            Assert.Equal(testPayload1, Encoding.UTF8.GetString(receivedMessage1.Payload));

            Message<byte[]> receivedMessage2 = (Message<byte[]>)Receive(input2);
            Assert.NotNull(receivedMessage2);
            Assert.Equal( testPayload1, Encoding.UTF8.GetString(receivedMessage2.Payload));

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
            Assert.Equal( testPayload2, Encoding.UTF8.GetString(receivedMessage1.Payload));
            receivedMessage1 = (Message<byte[]>)Receive(input1);
            Assert.NotNull(receivedMessage1);
            Assert.NotNull(receivedMessage1.Payload);

            receivedMessage2 = (Message<byte[]>)Receive(input2);
            Assert.NotNull(receivedMessage2);
            Assert.Equal(testPayload3, Encoding.UTF8.GetString(receivedMessage2.Payload));

            producerBinding.Unbind();
            binding1.Unbind();
            binding2.Unbind();
        }

        protected ILifecycle ExtractEndpoint(IBinding binding)
        {
            return GetFieldValue<ILifecycle>(binding, "_lifecycle");
        }

        protected PT GetFieldValue<PT>(object current, string name)
        {
            var fi = current.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return (PT)fi.GetValue(current);
        }

        protected PT GetPropertyValue<PT>(object current, string name)
        {
            var pi = current.GetType().GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return (PT)pi.GetValue(current);
        }
    }
}

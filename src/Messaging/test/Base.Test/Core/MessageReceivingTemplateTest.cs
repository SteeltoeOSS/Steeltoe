// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class MessageReceivingTemplateTest
    {
        private readonly TestMessagingTemplate template;

        public MessageReceivingTemplateTest()
        {
            template = new TestMessagingTemplate();
        }

        [Fact]
        public async Task ReceiveAsync()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync();

            Assert.Equal("home", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void Receive()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            var actual = template.Receive();

            Assert.Equal("home", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task ReceiveAsyncMissingDefaultDestination()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => template.ReceiveAsync());
        }

        [Fact]
        public void ReceiveMissingDefaultDestination()
        {
            Assert.Throws<InvalidOperationException>(() => template.Receive());
        }

        [Fact]
        public async Task ReceiveAsyncFromDestination()
        {
            IMessage expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync("somewhere");

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void ReceiveFromDestination()
        {
            IMessage expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var actual = template.Receive("somewhere");

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task ReceiveAsyncAndConvert()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>();
            Assert.Equal("home", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public void ReceiveAndConvert()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>();
            Assert.Equal("home", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public async Task ReceiveAndConvertAsyncFromDestination()
        {
            IMessage expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>("somewhere");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public void ReceiveAndConverFromDestination()
        {
            IMessage expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>("somewhere");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public async Task ReceiveAndConvertAsyncFailed()
        {
            IMessage expected = Message.Create("not a number test");
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            var ext = await Assert.ThrowsAsync<MessageConversionException>(() => template.ReceiveAndConvertAsync<int>("somewhere"));
            Assert.IsType<ConversionFailedException>(ext.InnerException);
        }

        [Fact]
        public void ReceiveAndConvertFailed()
        {
            IMessage expected = Message.Create("not a number test");
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            var ext = Assert.Throws<MessageConversionException>(() => template.ReceiveAndConvert<int>("somewhere"));
            Assert.IsType<ConversionFailedException>(ext.InnerException);
        }

        [Fact]
        public void ReceiveAndConvertNoConverter()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            try
            {
                template.ReceiveAndConvert<StringWriter>();
            }
            catch (MessageConversionException ex)
            {
                Assert.Contains("payload", ex.Message);
                Assert.Same(expected, ex.FailedMessage);
            }
        }

        [Fact]
        public async Task ReceiveAndConvertAsyncNoConverter()
        {
            IMessage expected = Message.Create("payload");
            template.DefaultReceiveDestination = "home";
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            try
            {
                await template.ReceiveAndConvertAsync<StringWriter>();
            }
            catch (MessageConversionException ex)
            {
                Assert.Contains("payload", ex.Message);
                Assert.Same(expected, ex.FailedMessage);
            }
        }

        internal class TestMessagingTemplate : AbstractMessagingTemplate<string>
        {
            public string Destination { get; set; }

            public IMessage ReceiveMessage { get; set; }

            protected override Task DoSendAsync(string destination, IMessage message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            protected override Task<IMessage> DoReceiveAsync(string destination, CancellationToken cancellationToken)
            {
                Destination = destination;
                return Task.FromResult(ReceiveMessage);
            }

            protected override Task<IMessage> DoSendAndReceiveAsync(string destination, IMessage requestMessage, CancellationToken cancellationToken)
            {
                Destination = destination;
                return Task.FromResult((IMessage)null);
            }

            protected override void DoSend(string destination, IMessage message)
            {
                return;
            }

            protected override IMessage DoReceive(string destination)
            {
                Destination = destination;
                return ReceiveMessage;
            }

            protected override IMessage DoSendAndReceive(string destination, IMessage requestMessage)
            {
                Destination = destination;
                return null;
            }
        }
    }
}

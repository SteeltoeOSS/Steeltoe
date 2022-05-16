// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class DestinationResolvingMessagingTemplateTest
    {
        private readonly TestDestinationResolvingMessagingTemplate template;

        private readonly TaskSchedulerSubscribableChannel myChannel;

        private readonly Dictionary<string, object> headers;

        private readonly TestMessagePostProcessor postProcessor;

        public DestinationResolvingMessagingTemplateTest()
        {
            var resolver = new TestMessageChannelDestinationResolver();

            myChannel = new TaskSchedulerSubscribableChannel();
            resolver.RegisterMessageChannel("myChannel", myChannel);

            template = new TestDestinationResolvingMessagingTemplate
            {
                DestinationResolver = resolver
            };

            headers = new Dictionary<string, object>() { { "key", "value" } };

            postProcessor = new TestMessagePostProcessor();
        }

        [Fact]
        public async Task SendAsync()
        {
            var message = Message.Create("payload");
            await template.SendAsync("myChannel", message);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public void Send()
        {
            var message = Message.Create("payload");
            template.Send("myChannel", message);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public Task SendAsyncNoDestinationResolver()
        {
            var template = new TestDestinationResolvingMessagingTemplate();
            return Assert.ThrowsAsync<InvalidOperationException>(() => template.SendAsync("myChannel", Message.Create("payload")));
        }

        [Fact]
        public void SendNoDestinationResolver()
        {
            var template = new TestDestinationResolvingMessagingTemplate();
            Assert.Throws<InvalidOperationException>(() => template.Send("myChannel", Message.Create("payload")));
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayload()
        {
            await template.ConvertAndSendAsync("myChannel", "payload");

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayload()
        {
            template.ConvertAndSend("myChannel", "payload");

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndHeaders()
        {
            await template.ConvertAndSendAsync("myChannel", "payload", headers);
            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadAndHeaders()
        {
            template.ConvertAndSend("myChannel", "payload", headers);
            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadWithPostProcessor()
        {
            await template.ConvertAndSendAsync("myChannel", "payload", postProcessor);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(postProcessor.Message, template.Message);
        }

        [Fact]
        public void ConvertAndSendPayloadWithPostProcessor()
        {
            template.ConvertAndSend("myChannel", "payload", postProcessor);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(postProcessor.Message, template.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndHeadersWithPostProcessor()
        {
            await template.ConvertAndSendAsync("myChannel", "payload", headers, postProcessor);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(postProcessor.Message, template.Message);
        }

        [Fact]
        public void ConvertAndSendPayloadAndHeadersWithPostProcessor()
        {
            template.ConvertAndSend("myChannel", "payload", headers, postProcessor);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(postProcessor.Message, template.Message);
        }

        [Fact]
        public async Task ReceiveAsync()
        {
            var expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync("myChannel");

            Assert.Same(expected, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void Receive()
        {
            var expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var actual = template.Receive("myChannel");

            Assert.Same(expected, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ReceiveAndConvertAsync()
        {
            var expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>("myChannel");

            Assert.Equal("payload", payload);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ReceiveAndConvert()
        {
            var expected = Message.Create("payload");
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>("myChannel");

            Assert.Equal("payload", payload);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task SendAndReceiveAsync()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.SendAndReceiveAsync("myChannel", requestMessage);

            Assert.Equal(requestMessage, template.Message);
            Assert.Same(responseMessage, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void SendAndReceive()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.SendAndReceive("myChannel", requestMessage);

            Assert.Equal(requestMessage, template.Message);
            Assert.Same(responseMessage, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsync()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.ConvertSendAndReceiveAsync<string>("myChannel", "request");

            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ConvertSendAndReceive()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.ConvertSendAndReceive<string>("myChannel", "request");

            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsyncWithHeaders()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.ConvertSendAndReceiveAsync<string>("myChannel", "request", headers);

            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ConvertSendAndReceiveWithHeaders()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.ConvertSendAndReceive<string>("myChannel", "request", headers);

            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsyncWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.ConvertSendAndReceiveAsync<string>("myChannel", "request", postProcessor);

            Assert.Equal("request", template.Message.Payload);
            Assert.Equal("request", postProcessor.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ConvertSendAndReceiveWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.ConvertSendAndReceive<string>("myChannel", "request", postProcessor);

            Assert.Equal("request", template.Message.Payload);
            Assert.Equal("request", postProcessor.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsyncWithHeadersAndPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.ConvertSendAndReceiveAsync<string>("myChannel", "request", headers, postProcessor);

            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("request", template.Message.Payload);
            Assert.Equal("request", postProcessor.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ConvertSendAndReceiveWithHeadersAndPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.ConvertSendAndReceive<string>("myChannel", "request", headers, postProcessor);

            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("request", template.Message.Payload);
            Assert.Equal("request", postProcessor.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        internal class TestDestinationResolvingMessagingTemplate : AbstractDestinationResolvingMessagingTemplate<IMessageChannel>
        {
            public TestDestinationResolvingMessagingTemplate()
                : base(null)
            {
            }

            public IMessageChannel MessageChannel { get; set; }

            public IMessage Message { get; set; }

            public IMessage ReceiveMessage { get; set; }

            protected override Task DoSendAsync(IMessageChannel channel, IMessage message, CancellationToken cancellationToken)
            {
                MessageChannel = channel;
                Message = message;
                return Task.CompletedTask;
            }

            protected override Task<IMessage> DoReceiveAsync(IMessageChannel channel, CancellationToken cancellationToken)
            {
                MessageChannel = channel;
                return Task.FromResult(ReceiveMessage);
            }

            protected override Task<IMessage> DoSendAndReceiveAsync(IMessageChannel channel, IMessage requestMessage, CancellationToken cancellationToken = default)
            {
                Message = requestMessage;
                MessageChannel = channel;
                return Task.FromResult(ReceiveMessage);
            }

            protected override void DoSend(IMessageChannel channel, IMessage message)
            {
                MessageChannel = channel;
                Message = message;
            }

            protected override IMessage DoReceive(IMessageChannel channel)
            {
                MessageChannel = channel;
                return ReceiveMessage;
            }

            protected override IMessage DoSendAndReceive(IMessageChannel channel, IMessage requestMessage)
            {
                Message = requestMessage;
                MessageChannel = channel;
                return ReceiveMessage;
            }
        }

        internal class TestMessageChannelDestinationResolver : IDestinationResolver<IMessageChannel>
        {
            private readonly IDictionary<string, IMessageChannel> channels = new Dictionary<string, IMessageChannel>();

            public void RegisterMessageChannel(string name, IMessageChannel channel)
            {
                channels.Add(name, channel);
            }

            public IMessageChannel ResolveDestination(string name)
            {
                channels.TryGetValue(name, out var chan);
                return chan;
            }

            object IDestinationResolver.ResolveDestination(string name)
            {
                return ResolveDestination(name);
            }
        }
    }
}

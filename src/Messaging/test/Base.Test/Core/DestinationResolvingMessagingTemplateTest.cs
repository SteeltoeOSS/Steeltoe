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

            template = new TestDestinationResolvingMessagingTemplate();
            template.DestinationResolver = resolver;

            headers = new Dictionary<string, object>() { { "key", "value" } };

            postProcessor = new TestMessagePostProcessor();
        }

        [Fact]
        public async Task SendAsync()
        {
            var message = new GenericMessage("payload");
            await template.SendAsync("myChannel", message);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public void Send()
        {
            var message = new GenericMessage("payload");
            template.Send("myChannel", message);

            Assert.Same(myChannel, template.MessageChannel);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public Task SendAsyncNoDestinationResolver()
        {
            var template = new TestDestinationResolvingMessagingTemplate();
            return Assert.ThrowsAsync<InvalidOperationException>(() => template.SendAsync("myChannel", new GenericMessage("payload")));
        }

        [Fact]
        public void SendNoDestinationResolver()
        {
            var template = new TestDestinationResolvingMessagingTemplate();
            Assert.Throws<InvalidOperationException>(() => template.Send("myChannel", new GenericMessage("payload")));
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
            var expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync("myChannel");

            Assert.Same(expected, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void Receive()
        {
            var expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var actual = template.Receive("myChannel");

            Assert.Same(expected, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ReceiveAndConvertAsync()
        {
            var expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>("myChannel");

            Assert.Equal("payload", payload);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ReceiveAndConvert()
        {
            var expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>("myChannel");

            Assert.Equal("payload", payload);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task SendAndReceiveAsync()
        {
            var requestMessage = new GenericMessage("request");
            var responseMessage = new GenericMessage("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.SendAndReceiveAsync("myChannel", requestMessage);

            Assert.Equal(requestMessage, template.Message);
            Assert.Same(responseMessage, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void SendAndReceive()
        {
            var requestMessage = new GenericMessage("request");
            var responseMessage = new GenericMessage("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.SendAndReceive("myChannel", requestMessage);

            Assert.Equal(requestMessage, template.Message);
            Assert.Same(responseMessage, actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsync()
        {
            var responseMessage = new GenericMessage("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.ConvertSendAndReceiveAsync<string>("myChannel", "request");

            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public void ConvertSendAndReceive()
        {
            var responseMessage = new GenericMessage("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.ConvertSendAndReceive<string>("myChannel", "request");

            Assert.Equal("request", template.Message.Payload);
            Assert.Same("response", actual);
            Assert.Same(myChannel, template.MessageChannel);
        }

        [Fact]
        public async Task ConvertSendAndReceiveAsyncWithHeaders()
        {
            var responseMessage = new GenericMessage("response");
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
            var responseMessage = new GenericMessage("response");
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
            var responseMessage = new GenericMessage("response");
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
            var responseMessage = new GenericMessage("response");
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
            var responseMessage = new GenericMessage("response");
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
            var responseMessage = new GenericMessage("response");
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

            protected override Task<IMessage> DoSendAndReceiveAsync(IMessageChannel channel, IMessage requestMessage, CancellationToken cancellationToken)
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

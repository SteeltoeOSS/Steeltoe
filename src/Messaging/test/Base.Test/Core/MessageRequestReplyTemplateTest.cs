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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class MessageRequestReplyTemplateTest
    {
        private readonly TestMessagingTemplate template;

        private readonly TestMessagePostProcessor postProcessor;

        private readonly Dictionary<string, object> headers;

        public MessageRequestReplyTemplateTest()
        {
            template = new TestMessagingTemplate();
            postProcessor = new TestMessagePostProcessor();
            headers = new Dictionary<string, object>() { { "key", "value" } };
        }

        [Fact]
        public async Task SendAndReceiveAsync()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;
            var actual = await template.SendAndReceiveAsync(requestMessage);

            Assert.Equal("home", template.Destination);
            Assert.Same(requestMessage, template.RequestMessage);
            Assert.Same(responseMessage, actual);
        }

        [Fact]
        public void SendAndReceive()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;
            var actual = template.SendAndReceive(requestMessage);

            Assert.Equal("home", template.Destination);
            Assert.Same(requestMessage, template.RequestMessage);
            Assert.Same(responseMessage, actual);
        }

        [Fact]
        public async Task SendAndReceiveAsyncMissingDestination()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => template.SendAndReceiveAsync(Message.Create("request")));
        }

        [Fact]
        public void SendAndReceiveMissingDestination()
        {
            Assert.Throws<InvalidOperationException>(() => template.SendAndReceive(Message.Create("request")));
        }

        [Fact]
        public async Task SendAndReceiveAsyncToDestination()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = await template.SendAndReceiveAsync("somewhere", requestMessage);
            Assert.Equal("somewhere", template.Destination);
            Assert.Same(requestMessage, template.RequestMessage);
            Assert.Same(responseMessage, actual);
        }

        [Fact]
        public void SendAndReceiveToDestination()
        {
            var requestMessage = Message.Create("request");
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var actual = template.SendAndReceive("somewhere", requestMessage);
            Assert.Equal("somewhere", template.Destination);
            Assert.Same(requestMessage, template.RequestMessage);
            Assert.Same(responseMessage, actual);
        }

        [Fact]
        public async Task ConvertAndSendAsync()
        {
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;

            var response = await template.ConvertSendAndReceiveAsync<string>("request");
            Assert.Equal("home", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public void ConvertAndSend()
        {
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;

            var response = template.ConvertSendAndReceive<string>("request");
            Assert.Equal("home", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public async Task ConvertAndSendAsyncToDestination()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = await template.ConvertSendAndReceiveAsync<string>("somewhere", "request");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public void ConvertAndSendToDestination()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = template.ConvertSendAndReceive<string>("somewhere", "request");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public async Task ConvertAndSendAsyncToDestinationWithHeaders()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = await template.ConvertSendAndReceiveAsync<string>("somewhere", "request", headers);
            Assert.Equal("somewhere", template.Destination);
            Assert.Equal("value", template.RequestMessage.Headers["key"]);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public void ConvertAndSendToDestinationWithHeaders()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = template.ConvertSendAndReceive<string>("somewhere", "request", headers);
            Assert.Equal("somewhere", template.Destination);
            Assert.Equal("value", template.RequestMessage.Headers["key"]);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
        }

        [Fact]
        public async Task ConvertAndSendAsyncWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;
            var response = await template.ConvertSendAndReceiveAsync<string>((object)"request", postProcessor);

            Assert.Equal("home", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        [Fact]
        public void ConvertAndSendWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.DefaultSendDestination = "home";
            template.ReceiveMessage = responseMessage;
            var response = template.ConvertSendAndReceive<string>((object)"request", postProcessor);

            Assert.Equal("home", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        [Fact]
        public async Task ConvertAndSendAsyncToDestinationWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = await template.ConvertSendAndReceiveAsync<string>("somewhere", "request", (IMessagePostProcessor)postProcessor);
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        [Fact]
        public void ConvertAndSendToDestinationWithPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = template.ConvertSendAndReceive<string>("somewhere", "request", (IMessagePostProcessor)postProcessor);
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        [Fact]
        public async Task ConvertAndSendAsyncToDestinationWithHeadersAndPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = await template.ConvertSendAndReceiveAsync<string>("somewhere", "request", headers, postProcessor);
            Assert.Equal("somewhere", template.Destination);
            Assert.Equal("value", template.RequestMessage.Headers["key"]);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        [Fact]
        public void ConvertAndSendToDestinationWithHeadersAndPostProcessor()
        {
            var responseMessage = Message.Create("response");
            template.ReceiveMessage = responseMessage;
            var response = template.ConvertSendAndReceive<string>("somewhere", "request", headers, postProcessor);
            Assert.Equal("somewhere", template.Destination);
            Assert.Equal("value", template.RequestMessage.Headers["key"]);
            Assert.Same("request", template.RequestMessage.Payload);
            Assert.Same("response", response);
            Assert.Same(postProcessor.Message, template.RequestMessage);
        }

        internal class TestMessagingTemplate : AbstractMessagingTemplate<string>
        {
            public string Destination { get; set; }

            public IMessage RequestMessage { get; set; }

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
                RequestMessage = requestMessage;
                return Task.FromResult(ReceiveMessage);
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
                RequestMessage = requestMessage;
                return ReceiveMessage;
            }
        }
    }
}

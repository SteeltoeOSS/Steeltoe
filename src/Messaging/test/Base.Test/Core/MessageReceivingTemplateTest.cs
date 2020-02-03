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

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class MessageReceivingTemplateTest
    {
        private TestMessagingTemplate template;

        public MessageReceivingTemplateTest()
        {
            template = new TestMessagingTemplate();
        }

        [Fact]
        public async Task ReceiveAsync()
        {
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync();

            Assert.Equal("home", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void Receive()
        {
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
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
            IMessage expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var actual = await template.ReceiveAsync("somewhere");

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void ReceiveFromDestination()
        {
            IMessage expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var actual = template.Receive("somewhere");

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task ReceiveAsyncAndConvert()
        {
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>();
            Assert.Equal("home", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public void ReceiveAndConvert()
        {
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>();
            Assert.Equal("home", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public async Task ReceiveAndConvertAsyncFromDestination()
        {
            IMessage expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var payload = await template.ReceiveAndConvertAsync<string>("somewhere");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public void ReceiveAndConverFromDestination()
        {
            IMessage expected = new GenericMessage("payload");
            template.ReceiveMessage = expected;
            var payload = template.ReceiveAndConvert<string>("somewhere");
            Assert.Equal("somewhere", template.Destination);
            Assert.Same("payload", payload);
        }

        [Fact]
        public async Task ReceiveAndConvertAsyncFailed()
        {
            IMessage expected = new GenericMessage("not a number test");
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            var ext = await Assert.ThrowsAsync<MessageConversionException>(() => template.ReceiveAndConvertAsync<int>("somewhere"));
            Assert.IsType<ConversionFailedException>(ext.InnerException);
        }

        [Fact]
        public void ReceiveAndConvertFailed()
        {
            IMessage expected = new GenericMessage("not a number test");
            template.ReceiveMessage = expected;
            template.MessageConverter = new GenericMessageConverter();
            var ext = Assert.Throws<MessageConversionException>(() => template.ReceiveAndConvert<int>("somewhere"));
            Assert.IsType<ConversionFailedException>(ext.InnerException);
        }

        [Fact]
        public void ReceiveAndConvertNoConverter()
        {
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
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
            IMessage expected = new GenericMessage("payload");
            template.DefaultDestination = "home";
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

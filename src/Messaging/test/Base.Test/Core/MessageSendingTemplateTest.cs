// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class MessageSendingTemplateTest
    {
        private readonly TestMessageSendingTemplate template;

        private readonly TestMessagePostProcessor postProcessor;

        private readonly Dictionary<string, object> headers;

        public MessageSendingTemplateTest()
        {
            template = new TestMessageSendingTemplate();
            postProcessor = new TestMessagePostProcessor();
            headers = new Dictionary<string, object>();
            headers.Add("key", "value");
        }

        [Fact]
        public async Task SendAsync()
        {
            var message = Message.Create("payload");
            template.DefaultSendDestination = "home";
            await template.SendAsync(message);

            Assert.Equal("home", template.Destination);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public async Task SendAsyncToDestination()
        {
            var message = Message.Create("payload");
            await template.SendAsync("somewhere", message);

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public async Task SendAsyncMissingDestination()
        {
            var message = Message.Create("payload");
            await Assert.ThrowsAsync<InvalidOperationException>(() => template.SendAsync(message));
        }

        [Fact]
        public async Task ConvertAndSendAsync()
        {
            await template.ConvertAndSendAsync("somewhere", "payload", headers, postProcessor);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayload()
        {
            template.DefaultSendDestination = "home";
            await template.ConvertAndSendAsync("payload");

            Assert.Equal("home", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadToDestination()
        {
            await template.ConvertAndSendAsync("somewhere", "payload");

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndHeadersToDestination()
        {
            await template.ConvertAndSendAsync("somewhere", "payload", headers);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndMutableHeadersToDestination()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;
            var messageHeaders = accessor.MessageHeaders;

            template.MessageConverter = new StringMessageConverter();
            await template.ConvertAndSendAsync("somewhere", "payload", messageHeaders);

            var actual = template.Message.Headers;
            Assert.Same(messageHeaders, actual);
            Assert.Equal(new MimeType("text", "plain", Encoding.UTF8), actual[MessageHeaders.CONTENT_TYPE]);
            Assert.Equal("bar", actual["foo"]);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadWithPostProcessor()
        {
            template.DefaultSendDestination = "home";
            await template.ConvertAndSendAsync((object)"payload", postProcessor);

            Assert.Equal("home", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadWithPostProcessorToDestination()
        {
            await template.ConvertAndSendAsync("somewhere", "payload", postProcessor);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncNoMatchingConverter()
        {
            var converter = new CompositeMessageConverter(new List<IMessageConverter>() { new NewtonJsonMessageConverter() });
            template.MessageConverter = converter;

            headers.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_XML);
            await Assert.ThrowsAsync<MessageConversionException>(() => template.ConvertAndSendAsync("home", "payload", new MessageHeaders(headers)));
        }

        [Fact]
        public void Send()
        {
            var message = Message.Create("payload");
            template.DefaultSendDestination = "home";
            template.Send(message);

            Assert.Equal("home", template.Destination);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public void SendToDestination()
        {
            var message = Message.Create("payload");
            template.Send("somewhere", message);

            Assert.Equal("somewhere", template.Destination);
            Assert.Same(message, template.Message);
        }

        [Fact]
        public void SendMissingDestination()
        {
            var message = Message.Create("payload");
            Assert.Throws<InvalidOperationException>(() => template.Send(message));
        }

        [Fact]
        public void ConvertAndSend()
        {
            template.ConvertAndSend("somewhere", "payload", headers, postProcessor);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendPayload()
        {
            template.DefaultSendDestination = "home";
            template.ConvertAndSend("payload");

            Assert.Equal("home", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadToDestination()
        {
            template.ConvertAndSend("somewhere", "payload");

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadAndHeadersToDestination()
        {
            template.ConvertAndSend("somewhere", "payload", headers);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal("value", template.Message.Headers["key"]);
            Assert.Equal("payload", template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadAndMutableHeadersToDestination()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;
            var messageHeaders = accessor.MessageHeaders;

            template.MessageConverter = new StringMessageConverter();
            template.ConvertAndSend("somewhere", "payload", messageHeaders);

            var actual = template.Message.Headers;
            Assert.Same(messageHeaders, actual);
            Assert.Equal(new MimeType("text", "plain", Encoding.UTF8), actual[MessageHeaders.CONTENT_TYPE]);
            Assert.Equal("bar", actual["foo"]);
        }

        [Fact]
        public void ConvertAndSendPayloadWithPostProcessor()
        {
            template.DefaultSendDestination = "home";
            template.ConvertAndSend((object)"payload", postProcessor);

            Assert.Equal("home", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendPayloadWithPostProcessorToDestination()
        {
            template.ConvertAndSend("somewhere", "payload", postProcessor);

            Assert.Equal("somewhere", template.Destination);
            Assert.NotNull(template.Message);
            Assert.Equal(2, template.Message.Headers.Count);
            Assert.Equal("payload", template.Message.Payload);

            Assert.NotNull(postProcessor.Message);
            Assert.Same(template.Message, postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendNoMatchingConverter()
        {
            var converter = new CompositeMessageConverter(new List<IMessageConverter>() { new NewtonJsonMessageConverter() });
            template.MessageConverter = converter;

            headers.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_XML);
            Assert.Throws<MessageConversionException>(() => template.ConvertAndSend("home", "payload", new MessageHeaders(headers)));
        }

        internal class TestMessageSendingTemplate : AbstractMessageSendingTemplate<string>
        {
            public string Destination;

            public IMessage Message;

            protected override Task DoSendAsync(string destination, IMessage message, CancellationToken cancellationToken)
            {
                Destination = destination;
                Message = message;
                return Task.CompletedTask;
            }

            protected override void DoSend(string destination, IMessage message)
            {
                Destination = destination;
                Message = message;
                return;
            }
        }
    }
}

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
using HeadersDictionary = System.Collections.Generic.IDictionary<string, object>;

namespace Steeltoe.Messaging.Core.Test
{
    public class MessageSendingTemplateTest
    {
        private readonly TestMessageSendingTemplate _template;

        private readonly TestMessagePostProcessor _postProcessor;

        private readonly Dictionary<string, object> _headers;

        public MessageSendingTemplateTest()
        {
            _template = new TestMessageSendingTemplate();
            _postProcessor = new TestMessagePostProcessor();
            _headers = new Dictionary<string, object>
            {
                { "key", "value" }
            };
        }

        [Fact]
        public async Task SendAsync()
        {
            var message = Message.Create("payload");
            _template.DefaultSendDestination = "home";
            await _template.SendAsync(message);

            Assert.Equal("home", _template.Destination);
            Assert.Same(message, _template.Message);
        }

        [Fact]
        public async Task SendAsyncToDestination()
        {
            var message = Message.Create("payload");
            await _template.SendAsync("somewhere", message);

            Assert.Equal("somewhere", _template.Destination);
            Assert.Same(message, _template.Message);
        }

        [Fact]
        public async Task SendAsyncMissingDestination()
        {
            var message = Message.Create("payload");
            await Assert.ThrowsAsync<InvalidOperationException>(() => _template.SendAsync(message));
        }

        [Fact]
        public async Task ConvertAndSendAsync()
        {
            await _template.ConvertAndSendAsync("somewhere", "payload", _headers, _postProcessor);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal("value", _template.Message.Headers["key"]);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayload()
        {
            _template.DefaultSendDestination = "home";
            await _template.ConvertAndSendAsync("payload");

            Assert.Equal("home", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadToDestination()
        {
            await _template.ConvertAndSendAsync("somewhere", "payload");

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndHeadersToDestination()
        {
            await _template.ConvertAndSendAsync("somewhere", "payload", _headers);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal("value", _template.Message.Headers["key"]);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadAndMutableHeadersToDestination()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;
            var messageHeaders = accessor.MessageHeaders;

            _template.MessageConverter = new StringMessageConverter();
            await _template.ConvertAndSendAsync("somewhere", "payload", messageHeaders);

            var actual = _template.Message.Headers;
            Assert.Same(messageHeaders, actual);
            Assert.Equal(new MimeType("text", "plain", Encoding.UTF8), actual[MessageHeaders.CONTENT_TYPE]);
            Assert.Equal("bar", actual["foo"]);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadWithPostProcessor()
        {
            _template.DefaultSendDestination = "home";
            await _template.ConvertAndSendAsync((object)"payload", _postProcessor);

            Assert.Equal("home", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncPayloadWithPostProcessorToDestination()
        {
            await _template.ConvertAndSendAsync("somewhere", "payload", _postProcessor);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public async Task ConvertAndSendAsyncNoMatchingConverter()
        {
            var converter = new CompositeMessageConverter(new List<IMessageConverter> { new NewtonJsonMessageConverter() });
            _template.MessageConverter = converter;

            _headers.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_XML);
            await Assert.ThrowsAsync<MessageConversionException>(() => _template.ConvertAndSendAsync("home", "payload", new MessageHeaders(_headers)));
        }

        [Fact]
        public void Send()
        {
            var message = Message.Create("payload");
            _template.DefaultSendDestination = "home";
            _template.Send(message);

            Assert.Equal("home", _template.Destination);
            Assert.Same(message, _template.Message);
        }

        [Fact]
        public void SendToDestination()
        {
            var message = Message.Create("payload");
            _template.Send("somewhere", message);

            Assert.Equal("somewhere", _template.Destination);
            Assert.Same(message, _template.Message);
        }

        [Fact]
        public void SendMissingDestination()
        {
            var message = Message.Create("payload");
            Assert.Throws<InvalidOperationException>(() => _template.Send(message));
        }

        [Fact]
        public void ConvertAndSend()
        {
            _template.ConvertAndSend("somewhere", "payload", _headers, _postProcessor);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal("value", _template.Message.Headers["key"]);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendPayload()
        {
            _template.DefaultSendDestination = "home";
            _template.ConvertAndSend("payload");

            Assert.Equal("home", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadToDestination()
        {
            _template.ConvertAndSend("somewhere", "payload");

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadAndHeadersToDestination()
        {
            _template.ConvertAndSend("somewhere", "payload", _headers);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal("value", _template.Message.Headers["key"]);
            Assert.Equal("payload", _template.Message.Payload);
        }

        [Fact]
        public void ConvertAndSendPayloadAndMutableHeadersToDestination()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;
            var messageHeaders = accessor.MessageHeaders;

            _template.MessageConverter = new StringMessageConverter();
            _template.ConvertAndSend("somewhere", "payload", messageHeaders);

            var actual = _template.Message.Headers;
            Assert.Same(messageHeaders, actual);
            Assert.Equal(new MimeType("text", "plain", Encoding.UTF8), actual[MessageHeaders.CONTENT_TYPE]);
            Assert.Equal("bar", actual["foo"]);
        }

        [Fact]
        public void ConvertAndSendPayloadWithPostProcessor()
        {
            _template.DefaultSendDestination = "home";
            _template.ConvertAndSend((object)"payload", _postProcessor);

            Assert.Equal("home", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendPayloadWithPostProcessorToDestination()
        {
            _template.ConvertAndSend("somewhere", "payload", _postProcessor);

            Assert.Equal("somewhere", _template.Destination);
            Assert.NotNull(_template.Message);
            Assert.Equal(2, ((HeadersDictionary)_template.Message.Headers).Count);
            Assert.Equal("payload", _template.Message.Payload);

            Assert.NotNull(_postProcessor.Message);
            Assert.Same(_template.Message, _postProcessor.Message);
        }

        [Fact]
        public void ConvertAndSendNoMatchingConverter()
        {
            var converter = new CompositeMessageConverter(new List<IMessageConverter> { new NewtonJsonMessageConverter() });
            _template.MessageConverter = converter;

            _headers.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_XML);
            Assert.Throws<MessageConversionException>(() => _template.ConvertAndSend("home", "payload", new MessageHeaders(_headers)));
        }

        internal class TestMessageSendingTemplate : AbstractMessageSendingTemplate<string>
        {
            public string Destination { get; set; }

            public IMessage Message { get; set; }

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
            }
        }
    }
}

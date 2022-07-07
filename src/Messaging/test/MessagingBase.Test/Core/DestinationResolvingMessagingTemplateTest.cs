// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test;

public class DestinationResolvingMessagingTemplateTest
{
    private readonly TestDestinationResolvingMessagingTemplate _template;

    private readonly TaskSchedulerSubscribableChannel _myChannel;

    private readonly Dictionary<string, object> _headers;

    private readonly TestMessagePostProcessor _postProcessor;

    public DestinationResolvingMessagingTemplateTest()
    {
        var resolver = new TestMessageChannelDestinationResolver();

        _myChannel = new TaskSchedulerSubscribableChannel();
        resolver.RegisterMessageChannel("myChannel", _myChannel);

        _template = new TestDestinationResolvingMessagingTemplate
        {
            DestinationResolver = resolver
        };

        _headers = new Dictionary<string, object> { { "key", "value" } };

        _postProcessor = new TestMessagePostProcessor();
    }

    [Fact]
    public async Task SendAsync()
    {
        var message = Message.Create("payload");
        await _template.SendAsync("myChannel", message);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.Same(message, _template.Message);
    }

    [Fact]
    public void Send()
    {
        var message = Message.Create("payload");
        _template.Send("myChannel", message);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.Same(message, _template.Message);
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
        await _template.ConvertAndSendAsync("myChannel", "payload");

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("payload", _template.Message.Payload);
    }

    [Fact]
    public void ConvertAndSendPayload()
    {
        _template.ConvertAndSend("myChannel", "payload");

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("payload", _template.Message.Payload);
    }

    [Fact]
    public async Task ConvertAndSendAsyncPayloadAndHeaders()
    {
        await _template.ConvertAndSendAsync("myChannel", "payload", _headers);
        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("payload", _template.Message.Payload);
    }

    [Fact]
    public void ConvertAndSendPayloadAndHeaders()
    {
        _template.ConvertAndSend("myChannel", "payload", _headers);
        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("payload", _template.Message.Payload);
    }

    [Fact]
    public async Task ConvertAndSendAsyncPayloadWithPostProcessor()
    {
        await _template.ConvertAndSendAsync("myChannel", "payload", _postProcessor);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("payload", _template.Message.Payload);

        Assert.NotNull(_postProcessor.Message);
        Assert.Same(_postProcessor.Message, _template.Message);
    }

    [Fact]
    public void ConvertAndSendPayloadWithPostProcessor()
    {
        _template.ConvertAndSend("myChannel", "payload", _postProcessor);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("payload", _template.Message.Payload);

        Assert.NotNull(_postProcessor.Message);
        Assert.Same(_postProcessor.Message, _template.Message);
    }

    [Fact]
    public async Task ConvertAndSendAsyncPayloadAndHeadersWithPostProcessor()
    {
        await _template.ConvertAndSendAsync("myChannel", "payload", _headers, _postProcessor);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("payload", _template.Message.Payload);

        Assert.NotNull(_postProcessor.Message);
        Assert.Same(_postProcessor.Message, _template.Message);
    }

    [Fact]
    public void ConvertAndSendPayloadAndHeadersWithPostProcessor()
    {
        _template.ConvertAndSend("myChannel", "payload", _headers, _postProcessor);

        Assert.Same(_myChannel, _template.MessageChannel);
        Assert.NotNull(_template.Message);
        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("payload", _template.Message.Payload);

        Assert.NotNull(_postProcessor.Message);
        Assert.Same(_postProcessor.Message, _template.Message);
    }

    [Fact]
    public async Task ReceiveAsync()
    {
        var expected = Message.Create("payload");
        _template.ReceiveMessage = expected;
        var actual = await _template.ReceiveAsync("myChannel");

        Assert.Same(expected, actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void Receive()
    {
        var expected = Message.Create("payload");
        _template.ReceiveMessage = expected;
        var actual = _template.Receive("myChannel");

        Assert.Same(expected, actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task ReceiveAndConvertAsync()
    {
        var expected = Message.Create("payload");
        _template.ReceiveMessage = expected;
        var payload = await _template.ReceiveAndConvertAsync<string>("myChannel");

        Assert.Equal("payload", payload);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void ReceiveAndConvert()
    {
        var expected = Message.Create("payload");
        _template.ReceiveMessage = expected;
        var payload = _template.ReceiveAndConvert<string>("myChannel");

        Assert.Equal("payload", payload);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task SendAndReceiveAsync()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.SendAndReceiveAsync("myChannel", requestMessage);

        Assert.Equal(requestMessage, _template.Message);
        Assert.Same(responseMessage, actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void SendAndReceive()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.SendAndReceive("myChannel", requestMessage);

        Assert.Equal(requestMessage, _template.Message);
        Assert.Same(responseMessage, actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task ConvertSendAndReceiveAsync()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.ConvertSendAndReceiveAsync<string>("myChannel", "request");

        Assert.Equal("request", _template.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void ConvertSendAndReceive()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.ConvertSendAndReceive<string>("myChannel", "request");

        Assert.Equal("request", _template.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task ConvertSendAndReceiveAsyncWithHeaders()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.ConvertSendAndReceiveAsync<string>("myChannel", "request", _headers);

        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("request", _template.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void ConvertSendAndReceiveWithHeaders()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.ConvertSendAndReceive<string>("myChannel", "request", _headers);

        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("request", _template.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task ConvertSendAndReceiveAsyncWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.ConvertSendAndReceiveAsync<string>("myChannel", "request", _postProcessor);

        Assert.Equal("request", _template.Message.Payload);
        Assert.Equal("request", _postProcessor.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void ConvertSendAndReceiveWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.ConvertSendAndReceive<string>("myChannel", "request", _postProcessor);

        Assert.Equal("request", _template.Message.Payload);
        Assert.Equal("request", _postProcessor.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public async Task ConvertSendAndReceiveAsyncWithHeadersAndPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.ConvertSendAndReceiveAsync<string>("myChannel", "request", _headers, _postProcessor);

        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("request", _template.Message.Payload);
        Assert.Equal("request", _postProcessor.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    [Fact]
    public void ConvertSendAndReceiveWithHeadersAndPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.ConvertSendAndReceive<string>("myChannel", "request", _headers, _postProcessor);

        Assert.Equal("value", _template.Message.Headers["key"]);
        Assert.Equal("request", _template.Message.Payload);
        Assert.Equal("request", _postProcessor.Message.Payload);
        Assert.Same("response", actual);
        Assert.Same(_myChannel, _template.MessageChannel);
    }

    internal sealed class TestDestinationResolvingMessagingTemplate : AbstractDestinationResolvingMessagingTemplate<IMessageChannel>
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

    internal sealed class TestMessageChannelDestinationResolver : IDestinationResolver<IMessageChannel>
    {
        private readonly IDictionary<string, IMessageChannel> _channels = new Dictionary<string, IMessageChannel>();

        public void RegisterMessageChannel(string name, IMessageChannel channel)
        {
            _channels.Add(name, channel);
        }

        public IMessageChannel ResolveDestination(string name)
        {
            _channels.TryGetValue(name, out var chan);
            return chan;
        }

        object IDestinationResolver.ResolveDestination(string name)
        {
            return ResolveDestination(name);
        }
    }
}

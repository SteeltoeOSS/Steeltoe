// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test;

public class MessageRequestReplyTemplateTest
{
    private readonly TestMessagingTemplate _template;

    private readonly TestMessagePostProcessor _postProcessor;

    private readonly Dictionary<string, object> _headers;

    public MessageRequestReplyTemplateTest()
    {
        _template = new TestMessagingTemplate();
        _postProcessor = new TestMessagePostProcessor();
        _headers = new Dictionary<string, object> { { "key", "value" } };
    }

    [Fact]
    public async Task SendAndReceiveAsync()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.SendAndReceiveAsync(requestMessage);

        Assert.Equal("home", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public void SendAndReceive()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        var actual = _template.SendAndReceive(requestMessage);

        Assert.Equal("home", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public async Task SendAndReceiveAsyncMissingDestination()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _template.SendAndReceiveAsync(Message.Create("request")));
    }

    [Fact]
    public void SendAndReceiveMissingDestination()
    {
        Assert.Throws<InvalidOperationException>(() => _template.SendAndReceive(Message.Create("request")));
    }

    [Fact]
    public async Task SendAndReceiveAsyncToDestination()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = await _template.SendAndReceiveAsync("somewhere", requestMessage);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public void SendAndReceiveToDestination()
    {
        var requestMessage = Message.Create("request");
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var actual = _template.SendAndReceive("somewhere", requestMessage);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public async Task ConvertAndSendAsync()
    {
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;

        var response = await _template.ConvertSendAndReceiveAsync<string>("request");
        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSend()
    {
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;

        var response = _template.ConvertSendAndReceive<string>("request");
        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestination()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request");
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSendToDestination()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = _template.ConvertSendAndReceive<string>("somewhere", "request");
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithHeaders()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _headers);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithHeaders()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = _template.ConvertSendAndReceive<string>("somewhere", "request", _headers);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        var response = await _template.ConvertSendAndReceiveAsync<string>((object)"request", _postProcessor);

        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        var response = _template.ConvertSendAndReceive<string>((object)"request", _postProcessor);

        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = _template.ConvertSendAndReceive<string>("somewhere", "request", _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithHeadersAndPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _headers, _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithHeadersAndPostProcessor()
    {
        var responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        var response = _template.ConvertSendAndReceive<string>("somewhere", "request", _headers, _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    internal sealed class TestMessagingTemplate : AbstractMessagingTemplate<string>
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

        protected override Task<IMessage> DoSendAndReceiveAsync(string destination, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            Destination = destination;
            RequestMessage = requestMessage;
            return Task.FromResult(ReceiveMessage);
        }

        protected override void DoSend(string destination, IMessage message)
        {
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        _headers = new Dictionary<string, object>
        {
            { "key", "value" }
        };
    }

    [Fact]
    public async Task SendAndReceiveAsync()
    {
        IMessage<string> requestMessage = Message.Create("request");
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        IMessage actual = await _template.SendAndReceiveAsync(requestMessage);

        Assert.Equal("home", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public void SendAndReceive()
    {
        IMessage<string> requestMessage = Message.Create("request");
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        IMessage actual = _template.SendAndReceive(requestMessage);

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
        IMessage<string> requestMessage = Message.Create("request");
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        IMessage actual = await _template.SendAndReceiveAsync("somewhere", requestMessage);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public void SendAndReceiveToDestination()
    {
        IMessage<string> requestMessage = Message.Create("request");
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        IMessage actual = _template.SendAndReceive("somewhere", requestMessage);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same(requestMessage, _template.RequestMessage);
        Assert.Same(responseMessage, actual);
    }

    [Fact]
    public async Task ConvertAndSendAsync()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;

        string response = await _template.ConvertSendAndReceiveAsync<string>("request");
        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSend()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;

        string response = _template.ConvertSendAndReceive<string>("request");
        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestination()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request");
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSendToDestination()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = _template.ConvertSendAndReceive<string>("somewhere", "request");
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithHeaders()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _headers);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithHeaders()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = _template.ConvertSendAndReceive<string>("somewhere", "request", _headers);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
    }

    [Fact]
    public async Task ConvertAndSendAsyncWithPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        string response = await _template.ConvertSendAndReceiveAsync<string>((object)"request", _postProcessor);

        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendWithPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.DefaultSendDestination = "home";
        _template.ReceiveMessage = responseMessage;
        string response = _template.ConvertSendAndReceive<string>((object)"request", _postProcessor);

        Assert.Equal("home", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = _template.ConvertSendAndReceive<string>("somewhere", "request", _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public async Task ConvertAndSendAsyncToDestinationWithHeadersAndPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = await _template.ConvertSendAndReceiveAsync<string>("somewhere", "request", _headers, _postProcessor);
        Assert.Equal("somewhere", _template.Destination);
        Assert.Equal("value", _template.RequestMessage.Headers["key"]);
        Assert.Same("request", _template.RequestMessage.Payload);
        Assert.Same("response", response);
        Assert.Same(_postProcessor.Message, _template.RequestMessage);
    }

    [Fact]
    public void ConvertAndSendToDestinationWithHeadersAndPostProcessor()
    {
        IMessage<string> responseMessage = Message.Create("response");
        _template.ReceiveMessage = responseMessage;
        string response = _template.ConvertSendAndReceive<string>("somewhere", "request", _headers, _postProcessor);
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

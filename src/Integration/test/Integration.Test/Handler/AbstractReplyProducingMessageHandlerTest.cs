// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Xunit;

namespace Steeltoe.Integration.Handler.Test;

public class AbstractReplyProducingMessageHandlerTest
{
    private readonly Mock<IMessageChannel> _mockChannel;
    private readonly TestAbstractReplyProducingMessageHandler _handler;
    private readonly IMessage _message;
    private readonly IServiceProvider _provider;

    public AbstractReplyProducingMessageHandlerTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();
        _handler = new TestAbstractReplyProducingMessageHandler(_provider.GetService<IApplicationContext>());
        _mockChannel = new Mock<IMessageChannel>();
        _message = IntegrationMessageBuilder.WithPayload("test").Build();
    }

    [Fact]
    public void ErrorMessageShouldContainChannelName()
    {
        _handler.OutputChannel = _mockChannel.Object;
        _mockChannel.Setup(c => c.Send(_message)).Returns(false);
        _mockChannel.Setup(c => c.ToString()).Returns("testChannel");

        try
        {
            _handler.HandleMessage(_message);
            throw new Exception("Expected a MessagingException");
        }
        catch (MessagingException e)
        {
            Assert.Contains("AbstractReplyProducingMessageHandler", e.Message);
        }
    }

    [Fact]
    public void TestNotPropagate()
    {
        _handler.ReturnValue = Message.Create("world", new Dictionary<string, object>
        {
            { "bar", "RAB" }
        });

        Assert.Empty(_handler.NotPropagatedHeaders);

        _handler.NotPropagatedHeaders = new List<string>
        {
            "f*",
            "*r"
        };

        _handler.OutputChannel = _mockChannel.Object;
        IMessage captor = null;
        _mockChannel.Setup(c => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, _) => captor = m);
        _mockChannel.Setup(c => c.ToString()).Returns("testChannel");

        _handler.HandleMessage(IntegrationMessageBuilder.WithPayload("hello").SetHeader("foo", "FOO").SetHeader("bar", "BAR").SetHeader("baz", "BAZ").Build());

        Assert.NotNull(captor);
        Assert.Null(captor.Headers.Get<string>("foo"));
        Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
        Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
    }

    [Fact]
    public void TestNotPropagateAddWhenNonExist()
    {
        _handler.ReturnValue = Message.Create("world", new Dictionary<string, object>
        {
            { "bar", "RAB" }
        });

        Assert.Empty(_handler.NotPropagatedHeaders);
        _handler.AddNotPropagatedHeaders("boom");
        _handler.OutputChannel = _mockChannel.Object;
        IMessage captor = null;
        _mockChannel.Setup(c => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, _) => captor = m);
        _mockChannel.Setup(c => c.ToString()).Returns("testChannel");

        _handler.HandleMessage(IntegrationMessageBuilder.WithPayload("hello").SetHeader("boom", "FOO").SetHeader("bar", "BAR").SetHeader("baz", "BAZ").Build());

        Assert.NotNull(captor);
        Assert.Null(captor.Headers.Get<string>("boom"));
        Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
        Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
    }

    [Fact]
    public void TestNotPropagateAdd()
    {
        _handler.ReturnValue = Message.Create("world", new Dictionary<string, object>
        {
            { "bar", "RAB" }
        });

        Assert.Empty(_handler.NotPropagatedHeaders);

        _handler.NotPropagatedHeaders = new List<string>
        {
            "foo"
        };

        _handler.AddNotPropagatedHeaders("b*r");
        _handler.OutputChannel = _mockChannel.Object;
        IMessage captor = null;
        _mockChannel.Setup(c => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, _) => captor = m);
        _mockChannel.Setup(c => c.ToString()).Returns("testChannel");

        _handler.HandleMessage(IntegrationMessageBuilder.WithPayload("hello").SetHeader("foo", "FOO").SetHeader("bar", "BAR").SetHeader("baz", "BAZ").Build());

        Assert.NotNull(captor);
        Assert.Null(captor.Headers.Get<string>("foo"));
        Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
        Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
    }

    private sealed class TestAbstractReplyProducingMessageHandler : AbstractReplyProducingMessageHandler
    {
        public object ReturnValue;

        public TestAbstractReplyProducingMessageHandler(IApplicationContext context)
            : base(context)
        {
        }

        public override void Initialize()
        {
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            if (ReturnValue != null)
            {
                return ReturnValue;
            }

            throw new NotImplementedException();
        }
    }
}

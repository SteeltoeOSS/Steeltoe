// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Xunit;

namespace Steeltoe.Integration.Handler.Test;

public class CollectionAndArrayTest
{
    private readonly TestAbstractReplyProducingMessageHandler _handler;
    private readonly IServiceProvider _provider;

    public CollectionAndArrayTest()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();
        _handler = new TestAbstractReplyProducingMessageHandler(_provider.GetService<IApplicationContext>());
    }

    [Fact]
    public void ListWithRequestReplyHandler()
    {
        _handler.ReturnValue = new List<string> { "foo", "bar" };
        var channel = new QueueChannel(_provider.GetService<IApplicationContext>());
        var message = IntegrationMessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
        _handler.HandleMessage(message);
        var reply1 = channel.Receive(0);
        var reply2 = channel.Receive(0);
        Assert.NotNull(reply1);
        Assert.Null(reply2);
        Assert.IsType<List<string>>(reply1.Payload);
        Assert.Equal(2, ((List<string>)reply1.Payload).Count);
    }

    [Fact]
    public void SetWithRequestReplyHandler()
    {
        _handler.ReturnValue = new HashSet<string>(new[] { "foo", "bar" });
        var channel = new QueueChannel(_provider.GetService<IApplicationContext>());
        var message = IntegrationMessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
        _handler.HandleMessage(message);
        var reply1 = channel.Receive(0);
        var reply2 = channel.Receive(0);
        Assert.NotNull(reply1);
        Assert.Null(reply2);
        Assert.IsType<HashSet<string>>(reply1.Payload);
        Assert.Equal(2, ((HashSet<string>)reply1.Payload).Count);
    }

    [Fact]
    public void ArrayWithRequestReplyHandler()
    {
        _handler.ReturnValue = new[] { "foo", "bar" };
        var channel = new QueueChannel(_provider.GetService<IApplicationContext>());
        var message = IntegrationMessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
        _handler.HandleMessage(message);
        var reply1 = channel.Receive(0);
        var reply2 = channel.Receive(0);
        Assert.NotNull(reply1);
        Assert.Null(reply2);
        Assert.IsType<string[]>(reply1.Payload);
        Assert.Equal(2, ((string[])reply1.Payload).Length);
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

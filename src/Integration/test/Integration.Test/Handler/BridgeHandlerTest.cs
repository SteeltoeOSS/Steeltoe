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

public class BridgeHandlerTest
{
    private readonly BridgeHandler _handler;
    private readonly IServiceProvider _provider;

    public BridgeHandlerTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();
        _handler = new BridgeHandler(_provider.GetService<IApplicationContext>());
    }

    [Fact]
    public void SimpleBridge()
    {
        var outputChannel = new QueueChannel(_provider.GetService<IApplicationContext>());
        _handler.OutputChannel = outputChannel;
        IMessage<string> request = Message.Create("test");
        _handler.HandleMessage(request);
        IMessage reply = outputChannel.Receive(0);
        Assert.NotNull(reply);
        Assert.Equal(request.Payload, reply.Payload);
        Assert.Equal(request.Headers, reply.Headers);
    }

    [Fact]
    public void MissingOutputChannelVerifiedAtRuntime()
    {
        IMessage<string> request = Message.Create("test");
        var ex = Assert.Throws<MessageHandlingException>(() => _handler.HandleMessage(request));
        Assert.IsType<DestinationResolutionException>(ex.InnerException);
    }

    [Fact]
    public void MissingOutputChannelAllowedForReplyChannelMessages()
    {
        var replyChannel = new QueueChannel(_provider.GetService<IApplicationContext>());
        IMessage request = IntegrationMessageBuilder.WithPayload("tst").SetReplyChannel(replyChannel).Build();
        _handler.HandleMessage(request);
        IMessage reply = replyChannel.Receive();
        Assert.NotNull(reply);
        Assert.Equal(request.Payload, reply.Payload);
        Assert.Equal(request.Headers, reply.Headers);
    }
}

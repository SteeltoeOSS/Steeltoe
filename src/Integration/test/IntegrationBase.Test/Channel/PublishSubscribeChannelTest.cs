// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class PublishSubscribeChannelTest
{
    private IServiceProvider _provider;

    public PublishSubscribeChannelTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void TestSend()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var provider = services.BuildServiceProvider();
        var handler = new CounterHandler();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(handler);
        var message = Message.Create("test");
        Assert.True(channel.Send(message));
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async ValueTask TestSendAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var provider = services.BuildServiceProvider();
        var handler = new CounterHandler();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(handler);
        var message = Message.Create("test");
        Assert.True(await channel.SendAsync(message));
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public void TestSendOneHandler_10_000_000()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var provider = services.BuildServiceProvider();
        var handler = new CounterHandler();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(handler);
        var message = Message.Create("test");
        Assert.True(channel.Send(message));
        for (var i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        Assert.Equal(10_000_001, handler.Count);
    }

    [Fact]
    public async ValueTask TestSendAsyncOneHandler_10_000_000()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var provider = services.BuildServiceProvider();
        var handler = new CounterHandler();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(handler);
        var message = Message.Create("test");
        Assert.True(await channel.SendAsync(message));
        for (var i = 0; i < 10_000_000; i++)
        {
            await channel.SendAsync(message);
        }

        Assert.Equal(10_000_001, handler.Count);
    }

    [Fact]
    public async ValueTask TestSendAsyncTwoHandler_10_000_000()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var provider = services.BuildServiceProvider();
        var handler1 = new CounterHandler();
        var handler2 = new CounterHandler();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(handler1);
        channel.Subscribe(handler2);
        var message = Message.Create("test");
        for (var i = 0; i < 10_000_000; i++)
        {
            await channel.SendAsync(message);
        }

        Assert.Equal(10_000_000, handler1.Count);
        Assert.Equal(10_000_000, handler2.Count);
    }

    private sealed class CounterHandler : IMessageHandler
    {
        public int Count;

        public string ServiceName { get; set; } = nameof(CounterHandler);

        public void HandleMessage(IMessage message)
        {
            Count++;
        }
    }
}

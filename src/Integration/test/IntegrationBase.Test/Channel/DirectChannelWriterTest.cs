// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Xunit;
using static Steeltoe.Integration.Channel.Test.DirectChannelTest;

namespace Steeltoe.Integration.Channel.Test;

public class DirectChannelWriterTest
{
    private readonly ServiceCollection _services;

    public DirectChannelWriterTest()
    {
        _services = new ServiceCollection();
        _services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var config = new ConfigurationBuilder().Build();
        _services.AddSingleton<IConfiguration>(config);
        _services.AddSingleton<IApplicationContext, GenericApplicationContext>();
    }

    [Fact]
    public async Task TestWriteAsync()
    {
        var provider = _services.BuildServiceProvider();
        var target = new ThreadNameExtractingTestTarget();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(target);
        var message = Message.Create("test");
        var currentId = Task.CurrentId;
        var curThreadId = Thread.CurrentThread.ManagedThreadId;
        await channel.Writer.WriteAsync(message);
        Assert.Equal(currentId, target.TaskId);
        Assert.Equal(curThreadId, target.ThreadId);
    }

    [Fact]
    public void TestTryWrite()
    {
        var provider = _services.BuildServiceProvider();
        var target = new ThreadNameExtractingTestTarget();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(target);
        var message = Message.Create("test");
        var currentId = Task.CurrentId;
        var curThreadId = Thread.CurrentThread.ManagedThreadId;
        Assert.True(channel.Writer.TryWrite(message));
        Assert.Equal(currentId, target.TaskId);
        Assert.Equal(curThreadId, target.ThreadId);
    }

    [Fact]
    public async Task TestWaitToWriteAsync()
    {
        var provider = _services.BuildServiceProvider();
        var target = new ThreadNameExtractingTestTarget();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        channel.Subscribe(target);
        Assert.True(await channel.Writer.WaitToWriteAsync());
        channel.Unsubscribe(target);
        Assert.False(await channel.Writer.WaitToWriteAsync());
    }

    [Fact]
    public void TestTryComplete()
    {
        var provider = _services.BuildServiceProvider();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        Assert.False(channel.Writer.TryComplete());
    }
}

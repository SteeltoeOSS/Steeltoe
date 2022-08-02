// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using Xunit;

namespace Steeltoe.Stream.Tck;

public class ErrorHandlingTest : AbstractTest
{
    private readonly IServiceCollection _container;

    public ErrorHandlingTest()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories);
    }

    [Fact]
    public async Task TestGlobalErrorWithMessage()
    {
        _container.AddStreamListeners<GlobalErrorHandlerWithErrorMessageConfig>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        DoSend(provider, message);

        var config = provider.GetService<GlobalErrorHandlerWithErrorMessageConfig>();
        Assert.True(config.GlobalErrorInvoked);
    }

    [Fact]
    public async Task TestGlobalErrorWithThrowable()
    {
        _container.AddStreamListeners<GlobalErrorHandlerWithExceptionConfig>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        DoSend(provider, message);

        var config = provider.GetService<GlobalErrorHandlerWithExceptionConfig>();
        Assert.True(config.GlobalErrorInvoked);
    }

    private void DoSend(ServiceProvider provider, IMessage<byte[]> message)
    {
        var source = provider.GetService<InputDestination>();
        source.Send(message);
    }
}

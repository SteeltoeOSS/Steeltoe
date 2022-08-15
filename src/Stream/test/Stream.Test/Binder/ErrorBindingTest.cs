// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class ErrorBindingTest : AbstractTest
{
    [Fact]
    public async Task TestErrorChannelNotBoundByDefault()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:defaultBinder=mock")
            .BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        IBinder binder = factory.GetBinder(null);
        Assert.NotNull(binder);
        Mock<IBinder> mock = Mock.Get(binder);
        mock.Verify(b => b.BindConsumer("input", null, It.IsAny<IMessageChannel>(), It.IsAny<ConsumerOptions>()));
        mock.Verify(b => b.BindProducer("output", It.IsAny<IMessageChannel>(), It.IsAny<ProducerOptions>()));
    }

    [Fact]
    public async Task TestConfigurationWithDefaultErrorHandler()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");

        ServiceCollection container =
            CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:bindings:input:consumer:maxAttempts=1");

        container.AddStreamListeners<ErrorConfigurationDefault>();
        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        SendAndValidate_ErrorConfigurationDefault(provider);
    }

    [Fact]
    public async Task TestConfigurationWithCustomErrorHandler()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");

        ServiceCollection container =
            CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:bindings:input:consumer:maxAttempts=1");

        container.AddStreamListeners<ErrorConfigurationWithCustomErrorHandler>();
        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        SendAndValidate_ErrorConfigurationWithCustomErrorHandler(provider);
    }

    private void SendAndValidate_ErrorConfigurationWithCustomErrorHandler(IServiceProvider provider)
    {
        var source = provider.GetService<InputDestination>();
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello1")));
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello2")));
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello3")));

        var errorConfig = provider.GetService<ErrorConfigurationWithCustomErrorHandler>();
        Assert.NotNull(errorConfig);
        Assert.Equal(6, errorConfig.Counter);
    }

    private void SendAndValidate_ErrorConfigurationDefault(IServiceProvider provider)
    {
        var source = provider.GetService<InputDestination>();
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello1")));
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello2")));
        source.Send(Message.Create(Encoding.UTF8.GetBytes("Hello3")));

        var errorConfig = provider.GetService<ErrorConfigurationDefault>();
        Assert.NotNull(errorConfig);
        Assert.Equal(3, errorConfig.Counter);
    }

    public class ErrorConfigurationDefault
    {
        public int Counter;

        [StreamListener("input")]
        public void Handle(object value)
        {
            Counter++;
            throw new Exception("BOOM!");
        }
    }

    public class ErrorConfigurationWithCustomErrorHandler
    {
        public int Counter;

        [StreamListener("input")]
        public void Handle(object value)
        {
            Counter++;
            throw new Exception("BOOM!");
        }

        [StreamListener("input.anonymous.errors")]
        public void Error(IMessage message)
        {
            Counter++;
        }
    }
}

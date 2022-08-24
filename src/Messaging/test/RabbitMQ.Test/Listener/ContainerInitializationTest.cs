// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Host;
using Xunit;
using RC = RabbitMQ.Client;
using SimpleMessageConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter.SimpleMessageConverter;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public sealed class ContainerInitializationTest : AbstractTest, IDisposable
{
    public const string TestMismatch = "test.mismatch";
    public const string TestMismatch2 = "test.mismatch2";

    private ServiceProvider _provider;

    [Fact]
    public async Task TestNoAdmin()
    {
        ServiceCollection services = CreateServiceCollection();
        services.AddRabbitQueue(new Queue(TestMismatch, false, false, true));
        services.AddSingleton(p => CreateMessageListenerContainer(p, TestMismatch));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());

        _provider = services.BuildServiceProvider();

        try
        {
            await _provider.GetRequiredService<IHostedService>().StartAsync(default);
        }
        catch (LifecycleException le)
        {
            Assert.IsType<InvalidOperationException>(le.InnerException.InnerException);
            Assert.Contains("mismatchedQueuesFatal", le.InnerException.InnerException.Message);
        }
    }

    [Fact]
    public async Task TestMismatchedQueue()
    {
        ServiceCollection services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TestMismatch));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TestMismatch, false, false, true));
        services.AddRabbitAdmin();

        _provider = services.BuildServiceProvider();

        try
        {
            await _provider.GetRequiredService<IHostedService>().StartAsync(default);
        }
        catch (LifecycleException le)
        {
            Assert.IsType<InvalidOperationException>(le.InnerException.InnerException);
            Assert.Contains("mismatchedQueuesFatal", le.InnerException.InnerException.Message);
        }
    }

    [Fact]
    public async Task TestMismatchedQueueDuringRestart()
    {
        ServiceCollection services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TestMismatch));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TestMismatch, true, false, false));
        services.AddRabbitAdmin();
        _provider = services.BuildServiceProvider();

        CountdownEvent[] latches = SetUpChannelLatches(_provider);
        await _provider.GetRequiredService<IHostedService>().StartAsync(default);
        var container = _provider.GetService<DirectMessageListenerContainer>();
        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));

        RabbitAdmin admin = _provider.GetRabbitAdmin();
        admin.RetryTemplate = null;
        admin.DeleteQueue(TestMismatch);
        Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
        admin.DeclareQueue(new Queue(TestMismatch, false, false, true));
        latches[2].Signal();
        Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

        int n = 0;

        while (n++ < 200 && container.IsRunning)
        {
            await Task.Delay(100);
        }

        Assert.False(container.IsRunning);
    }

    [Fact]
    public async Task TestMismatchedQueueDuringRestartMultiQueue()
    {
        ServiceCollection services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TestMismatch, TestMismatch2));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TestMismatch, true, false, false));
        services.AddRabbitQueue(new Queue(TestMismatch2, true, false, false));
        services.AddRabbitAdmin();
        _provider = services.BuildServiceProvider();

        CountdownEvent[] latches = SetUpChannelLatches(_provider);
        await _provider.GetRequiredService<IHostedService>().StartAsync(default);
        var container = _provider.GetService<DirectMessageListenerContainer>();
        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));

        RabbitAdmin admin = _provider.GetRabbitAdmin();
        admin.RetryTemplate = null;
        admin.DeleteQueue(TestMismatch);
        Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
        admin.DeclareQueue(new Queue(TestMismatch, false, false, true));
        latches[2].Signal();
        Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

        int n = 0;

        while (n++ < 200 && container.IsRunning)
        {
            await Task.Delay(100);
        }

        Assert.False(container.IsRunning);
    }

    public void Dispose()
    {
        if (_provider.GetService<IRabbitAdmin>() is RabbitAdmin admin)
        {
            admin.IgnoreDeclarationExceptions = true;

            try
            {
                admin.DeleteQueue(TestMismatch);
                admin.DeleteQueue(TestMismatch2);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        _provider.Dispose();
    }

    private CountdownEvent[] SetUpChannelLatches(IServiceProvider context)
    {
        var cf = context.GetService<IConnectionFactory>() as CachingConnectionFactory;
        var cancelLatch = new CountdownEvent(1);
        var mismatchLatch = new CountdownEvent(1);
        var preventContainerRedeclareQueueLatch = new CountdownEvent(1);
        var listener = new TestListener(cancelLatch, mismatchLatch, preventContainerRedeclareQueueLatch);
        cf.AddChannelListener(listener);

        return new[]
        {
            cancelLatch,
            mismatchLatch,
            preventContainerRedeclareQueueLatch
        };
    }

    private ServiceCollection CreateServiceCollection()
    {
        ServiceCollection services = CreateContainer();

        services.AddHostedService<RabbitHostService>();
        services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
        services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();
        services.TryAddSingleton<IConnectionFactory, CachingConnectionFactory>();
        services.TryAddSingleton<ISmartMessageConverter, SimpleMessageConverter>();
        return services;
    }

    private DirectMessageListenerContainer CreateMessageListenerContainer(IServiceProvider services, params string[] queueNames)
    {
        var cf = services.GetRequiredService<IConnectionFactory>();
        var ctx = services.GetRequiredService<IApplicationContext>();
        var listener = new TestMessageListener();
        var container = new DirectMessageListenerContainer(ctx, cf);
        container.SetQueueNames(queueNames);
        container.SetupMessageListener(listener);
        container.MismatchedQueuesFatal = true;

        return container;
    }

    private sealed class TestListener : IShutDownChannelListener
    {
        private readonly CountdownEvent _cancelLatch;
        private readonly CountdownEvent _mismatchLatch;
        private readonly CountdownEvent _preventContainerRedeclareQueueLatch;

        public TestListener(CountdownEvent cancelLatch, CountdownEvent mismatchLatch, CountdownEvent preventContainerRedeclareQueueLatch)
        {
            _cancelLatch = cancelLatch;
            _mismatchLatch = mismatchLatch;
            _preventContainerRedeclareQueueLatch = preventContainerRedeclareQueueLatch;
        }

        public void OnCreate(RC.IModel channel, bool transactional)
        {
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
            if (RabbitUtils.IsNormalChannelClose(args))
            {
                _cancelLatch.Signal();

                try
                {
                    _preventContainerRedeclareQueueLatch.Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
            else if (RabbitUtils.IsMismatchedQueueArgs(args))
            {
                _mismatchLatch.Signal();
            }
        }
    }

    private sealed class TestMessageListener : IMessageListener
    {
        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message)
        {
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
        }
    }
}

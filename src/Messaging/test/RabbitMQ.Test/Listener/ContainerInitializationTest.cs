// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Host;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public class ContainerInitializationTest : AbstractTest, IDisposable
{
    public const string TEST_MISMATCH = "test.mismatch";
    public const string TEST_MISMATCH2 = "test.mismatch2";

    private ServiceProvider provider;

    [Fact]
    public async Task TestNoAdmin()
    {
        var services = CreateServiceCollection();
        services.AddRabbitQueue(new Queue(TEST_MISMATCH, false, false, true));
        services.AddSingleton(p => CreateMessageListenerContainer(p, TEST_MISMATCH));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());

        provider = services.BuildServiceProvider();
        try
        {
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
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
        var services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TEST_MISMATCH));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TEST_MISMATCH, false, false, true));
        services.AddRabbitAdmin();

        provider = services.BuildServiceProvider();
        try
        {
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
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
        var services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TEST_MISMATCH));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TEST_MISMATCH, true, false, false));
        services.AddRabbitAdmin();
        provider = services.BuildServiceProvider();

        var latches = SetUpChannelLatches(provider);
        await provider.GetRequiredService<IHostedService>().StartAsync(default);
        var container = provider.GetService<DirectMessageListenerContainer>();
        Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

        var admin = provider.GetRabbitAdmin() as RabbitAdmin;
        admin.RetryTemplate = null;
        admin.DeleteQueue(TEST_MISMATCH);
        Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
        admin.DeclareQueue(new Queue(TEST_MISMATCH, false, false, true));
        latches[2].Signal();
        Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

        var n = 0;
        while (n++ < 200 && container.IsRunning)
        {
            await Task.Delay(100);
        }

        Assert.False(container.IsRunning);
    }

    [Fact]
    public async Task TestMismatchedQueueDuringRestartMultiQueue()
    {
        var services = CreateServiceCollection();
        services.AddSingleton(p => CreateMessageListenerContainer(p, TEST_MISMATCH, TEST_MISMATCH2));

        services.AddSingleton<ILifecycle>(p => p.GetService<DirectMessageListenerContainer>());
        services.AddRabbitQueue(new Queue(TEST_MISMATCH, true, false, false));
        services.AddRabbitQueue(new Queue(TEST_MISMATCH2, true, false, false));
        services.AddRabbitAdmin();
        provider = services.BuildServiceProvider();

        var latches = SetUpChannelLatches(provider);
        await provider.GetRequiredService<IHostedService>().StartAsync(default);
        var container = provider.GetService<DirectMessageListenerContainer>();
        Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

        var admin = provider.GetRabbitAdmin() as RabbitAdmin;
        admin.RetryTemplate = null;
        admin.DeleteQueue(TEST_MISMATCH);
        Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
        admin.DeclareQueue(new Queue(TEST_MISMATCH, false, false, true));
        latches[2].Signal();
        Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

        var n = 0;
        while (n++ < 200 && container.IsRunning)
        {
            await Task.Delay(100);
        }

        Assert.False(container.IsRunning);
    }

    public void Dispose()
    {
        if (provider.GetService<IRabbitAdmin>() is RabbitAdmin admin)
        {
            admin.IgnoreDeclarationExceptions = true;
            try
            {
                admin.DeleteQueue(TEST_MISMATCH);
                admin.DeleteQueue(TEST_MISMATCH2);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        provider.Dispose();
    }

    private CountdownEvent[] SetUpChannelLatches(IServiceProvider context)
    {
        var cf = context.GetService<IConnectionFactory>() as CachingConnectionFactory;
        var cancelLatch = new CountdownEvent(1);
        var mismatchLatch = new CountdownEvent(1);
        var preventContainerRedeclareQueueLatch = new CountdownEvent(1);
        var listener = new TestListener(cancelLatch, mismatchLatch, preventContainerRedeclareQueueLatch);
        cf.AddChannelListener(listener);
        return new[] { cancelLatch, mismatchLatch, preventContainerRedeclareQueueLatch };
    }

    private ServiceCollection CreateServiceCollection()
    {
        var services = CreateContainer();

        services.AddHostedService<RabbitHostService>();
        services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
        services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();
        services.TryAddSingleton<IConnectionFactory, CachingConnectionFactory>();
        services.TryAddSingleton<Converter.ISmartMessageConverter, RabbitMQ.Support.Converter.SimpleMessageConverter>();
        return services;
    }

    private sealed class TestListener : IShutDownChannelListener
    {
        private readonly CountdownEvent cancelLatch;
        private readonly CountdownEvent mismatchLatch;
        private readonly CountdownEvent preventContainerRedeclareQueueLatch;

        public TestListener(CountdownEvent cancelLatch, CountdownEvent mismatchLatch, CountdownEvent preventContainerRedeclareQueueLatch)
        {
            this.cancelLatch = cancelLatch;
            this.mismatchLatch = mismatchLatch;
            this.preventContainerRedeclareQueueLatch = preventContainerRedeclareQueueLatch;
        }

        public void OnCreate(RC.IModel channel, bool transactional)
        {
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
            if (RabbitUtils.IsNormalChannelClose(args))
            {
                cancelLatch.Signal();
                try
                {
                    preventContainerRedeclareQueueLatch.Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
            else if (RabbitUtils.IsMismatchedQueueArgs(args))
            {
                mismatchLatch.Signal();
            }
        }
    }

    private DirectMessageListenerContainer CreateMessageListenerContainer(IServiceProvider services, params string[] queueNames)
    {
        var cf = services.GetRequiredService<IConnectionFactory>();
        var ctx = services.GetRequiredService<IApplicationContext>();
        var queue2 = services.GetRequiredService<IQueue>();
        var listener = new TestMessageListener();
        var container = new DirectMessageListenerContainer(ctx, cf);
        container.SetQueueNames(queueNames);
        container.SetupMessageListener(listener);
        container.MismatchedQueuesFatal = true;

        return container;
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

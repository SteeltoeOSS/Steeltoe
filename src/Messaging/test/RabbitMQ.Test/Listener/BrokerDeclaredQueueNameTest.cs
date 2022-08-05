// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Host;
using Xunit;
using RC = RabbitMQ.Client;
using SimpleMessageConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter.SimpleMessageConverter;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public class BrokerDeclaredQueueNameTest : AbstractTest
{
    [Fact]
    public async Task TestBrokerNamedQueueDmlc()
    {
        var latch3 = new CountdownEvent(1);
        var latch4 = new CountdownEvent(2);
        var message = new AtomicReference<IMessage>();
        ServiceCollection services = CreateContainer();
        services.AddRabbitQueue(new Queue(string.Empty, false, true, true));

        services.AddHostedService<RabbitHostService>();
        services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
        services.TryAddSingleton<IConnectionFactory, CachingConnectionFactory>();
        services.TryAddSingleton<ISmartMessageConverter, SimpleMessageConverter>();
        services.AddSingleton(p => CreateDmlcContainer(p, latch3, latch4, message));
        services.AddRabbitAdmin();
        services.AddRabbitTemplate();
        await using ServiceProvider provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IHostedService>().StartAsync(default);

        using var container = provider.GetRequiredService<DirectMessageListenerContainer>();
        using var cf = provider.GetRequiredService<IConnectionFactory>() as CachingConnectionFactory;

        await container.StartAsync();
        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10))); // Really wait for container to start

        var queue = provider.GetRequiredService<IQueue>();
        using RabbitTemplate template = provider.GetRabbitTemplate();
        string firstActualName = queue.ActualName;
        message.Value = null;
        template.ConvertAndSend(firstActualName, "foo");

        Assert.True(latch3.Wait(TimeSpan.FromSeconds(10)));
        string body = EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Value.Payload);
        Assert.Equal("foo", body);
        var newConnectionLatch = new CountdownEvent(2);
        var conListener = new TestConnectionListener(newConnectionLatch);
        cf.AddConnectionListener(conListener);
        cf.ResetConnection();
        Assert.True(newConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
        string secondActualName = queue.ActualName;
        Assert.NotEqual(firstActualName, secondActualName);
        message.Value = null;
        template.ConvertAndSend(secondActualName, "bar");
        Assert.True(latch4.Wait(TimeSpan.FromSeconds(10)));
        body = EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Value.Payload);
        Assert.Equal("bar", body);
        await container.StopAsync();
    }

    private DirectMessageListenerContainer CreateDmlcContainer(IServiceProvider services, CountdownEvent latch3, CountdownEvent latch4,
        AtomicReference<IMessage> message)
    {
        var cf = services.GetRequiredService<IConnectionFactory>();
        var ctx = services.GetRequiredService<IApplicationContext>();
        var queue2 = services.GetRequiredService<IQueue>();
        var listener = new TestMessageListener(latch3, latch4, message);
        var container = new DirectMessageListenerContainer(ctx, cf);
        container.SetQueues(queue2);
        container.SetupMessageListener(listener);
        container.FailedDeclarationRetryInterval = 1000;
        container.MissingQueuesFatal = false;
        container.RecoveryInterval = 100;
        container.IsAutoStartup = false;

        return container;
    }

    private sealed class TestConnectionListener : IConnectionListener
    {
        public CountdownEvent Latch { get; }

        public TestConnectionListener(CountdownEvent latch)
        {
            Latch = latch;
        }

        public void OnClose(IConnection connection)
        {
        }

        public void OnCreate(IConnection connection)
        {
            if (!Latch.IsSet)
            {
                Latch.Signal();
            }
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
        }
    }

    private sealed class TestMessageListener : IMessageListener
    {
        public AcknowledgeMode ContainerAckMode { get; set; }

        public CountdownEvent Latch1 { get; }

        public CountdownEvent Latch2 { get; }

        public AtomicReference<IMessage> Message { get; }

        public TestMessageListener(CountdownEvent latch1, CountdownEvent latch2, AtomicReference<IMessage> message)
        {
            Latch1 = latch1;
            Latch2 = latch2;
            Message = message;
        }

        public void OnMessage(IMessage message)
        {
            Message.Value = message;

            if (!Latch1.IsSet)
            {
                Latch1.Signal();
            }

            if (!Latch2.IsSet)
            {
                Latch2.Signal();
            }
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            throw new NotImplementedException();
        }
    }
}

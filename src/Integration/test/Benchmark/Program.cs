// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;

namespace Benchmark;

[MemoryDiagnoser]
public sealed class Program
{
    // |                                                Method |     Mean |   Error |  StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |------------------------------------------------------ |---------:|--------:|--------:|------:|------:|------:|----------:|
    // |           DirectChannel_Send_10_000_000_SingleHandler | 229.1 ms | 2.47 ms | 2.31 ms |     - |     - |     - |   5.97 KB |
    // |      DirectChannel_SendAsync_10_000_000_SingleHandler | 233.5 ms | 1.50 ms | 1.33 ms |     - |     - |     - |   4.73 KB |
    // |             DirectChannel_Send_10_000_000_TwoHandlers | 472.9 ms | 5.28 ms | 4.41 ms |     - |     - |     - |    6.6 KB |
    // |            DirectChannel_Send_10_000_000_FourHandlers | 469.4 ms | 2.77 ms | 2.45 ms |     - |     - |     - |   5.29 KB |
    // | PublishSubscribeChannel_Send_10_000_000_SingleHandler | 387.0 ms | 2.89 ms | 2.56 ms |     - |     - |     - |   6.13 KB |
    // |   PublishSubscribeChannel_Send_10_000_000_TwoHandlers | 468.5 ms | 3.60 ms | 3.37 ms |     - |     - |     - |   6.32 KB |

    private static void Main()
    {
        BenchmarkRunner.Run<Program>();
    }

    [Benchmark]
    public void DirectChannel_Send_10_000_000_SingleHandler()
    {
        var services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var directChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        var handler = new CounterHandler();

        directChannel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");
        directChannel.Send(message);

        for (int i = 0; i < 10_000_000; i++)
        {
            directChannel.Send(message);
        }

        if (handler.Count != 10_000_000 + 1)
        {
            throw new InvalidOperationException("Handler count invalid");
        }
    }

    [Benchmark]
    public async Task DirectChannel_SendAsync_10_000_000_SingleHandlerAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var directChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        var handler = new CounterHandler();

        directChannel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");
        await directChannel.SendAsync(message);

        for (int i = 0; i < 10_000_000; i++)
        {
            await directChannel.SendAsync(message);
        }

        if (handler.Count != 10_000_000 + 1)
        {
            throw new InvalidOperationException("Handler count invalid");
        }
    }

    [Benchmark]
    public void DirectChannel_Send_10_000_000_TwoHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        var count1 = new CounterHandler();
        var count2 = new CounterHandler();
        channel.Subscribe(count1);
        channel.Subscribe(count2);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        if (count1.Count != 5_000_000)
        {
            throw new InvalidOperationException("Handler count1 invalid");
        }

        if (count2.Count != 5_000_000)
        {
            throw new InvalidOperationException("Handler count2 invalid");
        }
    }

    [Benchmark]
    public void DirectChannel_Send_10_000_000_FourHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new DirectChannel(provider.GetService<IApplicationContext>());
        var count1 = new CounterHandler();
        var count2 = new CounterHandler();
        var count3 = new CounterHandler();
        var count4 = new CounterHandler();
        channel.Subscribe(count1);
        channel.Subscribe(count2);
        channel.Subscribe(count3);
        channel.Subscribe(count4);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        if (count1.Count != 10_000_000 / 4)
        {
            throw new InvalidOperationException("Handler count1 invalid");
        }

        if (count2.Count != 10_000_000 / 4)
        {
            throw new InvalidOperationException("Handler count2 invalid");
        }

        if (count3.Count != 10_000_000 / 4)
        {
            throw new InvalidOperationException("Handler count3 invalid");
        }

        if (count4.Count != 10_000_000 / 4)
        {
            throw new InvalidOperationException("Handler count4 invalid");
        }
    }

    [Benchmark]
    public void PublishSubscribeChannel_Send_10_000_000_SingleHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        var handler = new CounterHandler();

        channel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");
        channel.Send(message);

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        if (handler.Count != 10_000_000 + 1)
        {
            throw new InvalidOperationException("Handler count invalid");
        }
    }

    [Benchmark]
    public void PublishSubscribeChannel_Send_10_000_000_TwoHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        var handler1 = new CounterHandler();
        var handler2 = new CounterHandler();
        channel.Subscribe(handler1);
        channel.Subscribe(handler2);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        if (handler1.Count != 10_000_000)
        {
            throw new InvalidOperationException("Handler count1 invalid");
        }

        if (handler2.Count != 10_000_000)
        {
            throw new InvalidOperationException("Handler count2 invalid");
        }
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

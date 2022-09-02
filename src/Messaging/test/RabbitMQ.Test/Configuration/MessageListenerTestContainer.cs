// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Messaging.RabbitMQ.Listener;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public sealed class MessageListenerTestContainer : IMessageListenerContainer
{
    internal bool StopInvoked { get; set; }
    internal bool StartInvoked { get; set; }
    internal bool DestroyInvoked { get; set; }
    internal bool InitializationInvoked { get; set; }
    internal IRabbitListenerEndpoint Endpoint { get; }

    public bool IsStarted => StartInvoked && InitializationInvoked;

    public bool IsStopped => StopInvoked && DestroyInvoked;

    public bool IsAutoStartup => true;

    public bool IsRunning => StartInvoked && !StopInvoked;

    public int Phase => 0;

    public string ServiceName { get; set; } = nameof(MessageListenerTestContainer);

    public MessageListenerTestContainer(IRabbitListenerEndpoint endpoint)
    {
        Endpoint = endpoint;
    }

    public void Dispose()
    {
        if (!StopInvoked)
        {
            StopAsync().Wait();
        }

        DestroyInvoked = true;
    }

    public void Initialize()
    {
        InitializationInvoked = true;
    }

    public void LazyLoad()
    {
    }

    public void SetupMessageListener(IMessageListener messageListener)
    {
    }

    public Task StartAsync()
    {
        if (!InitializationInvoked)
        {
            throw new InvalidOperationException($"afterPropertiesSet should have been invoked before start on {this}");
        }

        if (StartInvoked)
        {
            throw new InvalidOperationException($"Start already invoked on {this}");
        }

        StartInvoked = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(Action callback)
    {
        StopInvoked = true;
        callback();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (StopInvoked)
        {
            throw new InvalidOperationException($"Stop already invoked on {this}");
        }

        StopInvoked = true;
        return Task.CompletedTask;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("TestContainer{");
        sb.Append("endpoint=").Append(Endpoint);
        sb.Append(", startInvoked=").Append(StartInvoked);
        sb.Append(", initializationInvoked=").Append(InitializationInvoked);
        sb.Append(", stopInvoked=").Append(StopInvoked);
        sb.Append(", destroyInvoked=").Append(DestroyInvoked);
        sb.Append('}');
        return sb.ToString();
    }
}

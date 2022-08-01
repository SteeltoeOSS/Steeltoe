// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Listener;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public sealed class MessageListenerTestContainer : IMessageListenerContainer
{
    internal bool StopInvoked;
    internal bool StartInvoked;
    internal bool DestroyInvoked;
    internal bool InitializationInvoked;
    internal IRabbitListenerEndpoint Endpoint;

    public MessageListenerTestContainer(IRabbitListenerEndpoint endpoint)
    {
        Endpoint = endpoint;
    }

    public bool IsStarted => StartInvoked && InitializationInvoked;

    public bool IsStopped => StopInvoked && DestroyInvoked;

    public bool IsAutoStartup => true;

    public bool IsRunning
    {
        get => StartInvoked && !StopInvoked;
    }

    public int Phase => 0;

    public string ServiceName { get; set; } = nameof(MessageListenerTestContainer);

    public void Dispose()
    {
        if (!StopInvoked)
        {
            Stop().Wait();
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

    public Task Start()
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

    public Task Stop(Action callback)
    {
        StopInvoked = true;
        callback();
        return Task.CompletedTask;
    }

    public Task Stop()
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Stream.Configuration;

namespace Steeltoe.Stream.Binder.Test;

public abstract class AbstractTestBinder<TBinder> : IBinder<IMessageChannel>
    where TBinder : AbstractBinder<IMessageChannel>
{
    protected HashSet<string> Queues { get; } = new();

    protected HashSet<string> Exchanges { get; } = new();

    public Type TargetType => typeof(IMessageChannel);

    public TBinder CoreBinder { get; private set; }

    public TBinder Binder
    {
        get => CoreBinder;
        set => CoreBinder = value;
    }

    public IApplicationContext ApplicationContext => Binder?.ApplicationContext;

    public string ServiceName
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual IBinding BindConsumer(string name, string group, IMessageChannel inboundTarget, IConsumerOptions consumerOptions)
    {
        CheckChannelIsConfigured(inboundTarget, consumerOptions);
        return BindConsumer(name, group, (object)inboundTarget, consumerOptions);
    }

    public virtual IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
    {
        Queues.Add(name);
        return CoreBinder.BindConsumer(name, group, inboundTarget, consumerOptions);
    }

    public virtual IBinding BindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
    {
        return BindProducer(name, (object)outboundTarget, producerOptions);
    }

    public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
    {
        Queues.Add(name);
        return CoreBinder.BindProducer(name, outboundTarget, producerOptions);
    }

    public abstract void Cleanup();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    private void CheckChannelIsConfigured(IMessageChannel messageChannel, IConsumerOptions options)
    {
        if (messageChannel is AbstractSubscribableChannel subChan && !options.UseNativeDecoding && subChan.ChannelInterceptors.Count == 0)
        {
            throw new InvalidOperationException(
                "'messageChannel' appears to be misconfigured. Consider creating channel via AbstractBinderTest.createBindableChannel(..)");
        }
    }
}

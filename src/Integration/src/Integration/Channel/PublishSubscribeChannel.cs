// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Channel;

public class PublishSubscribeChannel : AbstractTaskSchedulerChannel
{
    protected BroadcastingDispatcher BroadcastingDispatcher => (BroadcastingDispatcher)Dispatcher;

    public override string ComponentType => "publish-subscribe-channel";

    public virtual IErrorHandler ErrorHandler
    {
        get => BroadcastingDispatcher.ErrorHandler;
        set => BroadcastingDispatcher.ErrorHandler = value;
    }

    public virtual bool IgnoreFailures
    {
        get => BroadcastingDispatcher.IgnoreFailures;
        set => BroadcastingDispatcher.IgnoreFailures = value;
    }

    public virtual bool ApplySequence
    {
        get => BroadcastingDispatcher.ApplySequence;
        set => BroadcastingDispatcher.ApplySequence = value;
    }

    public virtual int MinSubscribers
    {
        get => BroadcastingDispatcher.MinSubscribers;
        set => BroadcastingDispatcher.MinSubscribers = value;
    }

    public PublishSubscribeChannel(ILogger logger = null)
        : this(null, null, null, logger)
    {
    }

    public PublishSubscribeChannel(IApplicationContext context, ILogger logger = null)
        : this(context, null, null, logger)
    {
    }

    public PublishSubscribeChannel(IApplicationContext context, string name, ILogger logger = null)
        : this(context, null, name, logger)
    {
    }

    public PublishSubscribeChannel(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
        : this(context, executor, null, logger)
    {
    }

    public PublishSubscribeChannel(IApplicationContext context, TaskScheduler executor, string name, ILogger logger = null)
        : base(context, new BroadcastingDispatcher(context, executor), executor, name, logger)
    {
        BroadcastingDispatcher.IgnoreFailures = false;
        BroadcastingDispatcher.ApplySequence = false;
        BroadcastingDispatcher.MinSubscribers = 0;
        Dispatcher.MessageHandlingDecorator = new MessageHandlingDecorator(this);
        Writer = new PublishSubscribeChannelWriter(this, logger);
        Reader = new NotSupportedChannelReader();
    }
}

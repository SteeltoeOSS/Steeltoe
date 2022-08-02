// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

public class ReplyProducingMessageHandlerWrapper : AbstractReplyProducingMessageHandler, ILifecycle
{
    private readonly IMessageHandler _target;

    public bool IsRunning => _target is not ILifecycle lifecycle || lifecycle.IsRunning;

    public ReplyProducingMessageHandlerWrapper(IApplicationContext context, IMessageHandler target)
        : base(context)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public Task Start()
    {
        if (_target is ILifecycle lifeCycle)
        {
            return lifeCycle.Start();
        }

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (_target is ILifecycle lifeCycle)
        {
            return lifeCycle.Stop();
        }

        return Task.CompletedTask;
    }

    public override void Initialize()
    {
        // Nothing to do
    }

    protected override object HandleRequestMessage(IMessage requestMessage)
    {
        _target.HandleMessage(requestMessage);
        return null;
    }
}

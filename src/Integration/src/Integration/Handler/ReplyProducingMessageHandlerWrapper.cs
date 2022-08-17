// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
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
        ArgumentGuard.NotNull(target);

        _target = target;
    }

    public Task StartAsync()
    {
        if (_target is ILifecycle lifeCycle)
        {
            return lifeCycle.StartAsync();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_target is ILifecycle lifeCycle)
        {
            return lifeCycle.StopAsync();
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

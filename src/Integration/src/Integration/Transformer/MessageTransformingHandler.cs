// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Transformer;

public class MessageTransformingHandler : AbstractReplyProducingMessageHandler, ILifecycle
{
    protected override bool ShouldCopyRequestHeaders => false;

    public ITransformer Transformer { get; }

    public bool IsRunning
    {
        get
        {
            if (Transformer is ILifecycle asLifecycle)
            {
                return asLifecycle.IsRunning;
            }

            return false;
        }
    }

    public MessageTransformingHandler(IApplicationContext context, ITransformer transformer)
        : base(context)
    {
        ArgumentGuard.NotNull(transformer);

        Transformer = transformer;
        RequiresReply = true;
    }

    public override void Initialize()
    {
        // Nothing to do
    }

    public override void AddNotPropagatedHeaders(params string[] headers)
    {
        base.AddNotPropagatedHeaders(headers);
        PopulateNotPropagatedHeadersIfAny();
    }

    public Task StartAsync()
    {
        if (Transformer is ILifecycle asLifecycle)
        {
            return asLifecycle.StartAsync();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (Transformer is ILifecycle asLifecycle)
        {
            return asLifecycle.StopAsync();
        }

        return Task.CompletedTask;
    }

    protected override object HandleRequestMessage(IMessage requestMessage)
    {
        try
        {
            return Transformer.Transform(requestMessage);
        }
        catch (Exception e)
        {
            if (e is MessageTransformationException)
            {
                throw;
            }

            throw new MessageTransformationException(requestMessage, $"Failed to transform Message in {this}", e);
        }
    }

    private void PopulateNotPropagatedHeadersIfAny()
    {
        // var notPropagatedHeaders = NotPropagatedHeaders;
        //
        // if (Transformer is AbstractMessageProcessingTransformer && notPropagatedHeaders.Count != 0)
        // {
        //    ((AbstractMessageProcessingTransformer)this.Transformer)
        //            .setNotPropagatedHeaders(notPropagatedHeaders.toArray(new String[0]));
        // }
    }
}

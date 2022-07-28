// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Binding;

internal sealed class PartitioningInterceptor : AbstractChannelInterceptor
{
    internal readonly IBindingOptions BindingOptions;

    internal readonly PartitionHandler PartitionHandler;
    internal readonly IMessageBuilderFactory MessageBuilderFactory = new MutableIntegrationMessageBuilderFactory();

    private readonly IExpressionParser _expressionParser;
    private readonly IEvaluationContext _evaluationContext;

    public PartitioningInterceptor(IExpressionParser expressionParser, IEvaluationContext evaluationContext, IBindingOptions bindingOptions, IPartitionKeyExtractorStrategy partitionKeyExtractorStrategy, IPartitionSelectorStrategy partitionSelectorStrategy)
    {
        BindingOptions = bindingOptions;
        _expressionParser = expressionParser;
        _evaluationContext = evaluationContext;
        PartitionHandler = new PartitionHandler(
            expressionParser,
            evaluationContext,
            BindingOptions.Producer,
            partitionKeyExtractorStrategy,
            partitionSelectorStrategy);
    }

    public int PartitionCount
    {
        get { return PartitionHandler.PartitionCount; }
        set { PartitionHandler.PartitionCount = value; }
    }

    public override IMessage PreSend(IMessage message, IMessageChannel channel)
    {
        var objMessage = message as IMessage<object> ?? Message.Create(message.Payload, message.Headers); // Primitives are not covariant with out T, so box the primitive ...

        if (!message.Headers.ContainsKey(BinderHeaders.PartitionOverride))
        {
            var partition = PartitionHandler.DeterminePartition(message);
            return MessageBuilderFactory
                .FromMessage(objMessage)
                .SetHeader(BinderHeaders.PartitionHeader, partition).Build();
        }
        else
        {
            return MessageBuilderFactory
                .FromMessage(objMessage)
                .SetHeader(BinderHeaders.PartitionHeader, message.Headers[BinderHeaders.PartitionOverride])
                .RemoveHeader(BinderHeaders.PartitionOverride).Build();
        }
    }
}

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
    internal readonly IBindingOptions _bindingOptions;

    internal readonly PartitionHandler _partitionHandler;
    internal readonly IMessageBuilderFactory _messageBuilderFactory = new MutableIntegrationMessageBuilderFactory();

    private readonly IExpressionParser _expressionParser;
    private readonly IEvaluationContext _evaluationContext;

    public PartitioningInterceptor(IExpressionParser expressionParser, IEvaluationContext evaluationContext, IBindingOptions bindingOptions, IPartitionKeyExtractorStrategy partitionKeyExtractorStrategy, IPartitionSelectorStrategy partitionSelectorStrategy)
    {
        _bindingOptions = bindingOptions;
        _expressionParser = expressionParser;
        _evaluationContext = evaluationContext;
        _partitionHandler = new PartitionHandler(
            expressionParser,
            evaluationContext,
            _bindingOptions.Producer,
            partitionKeyExtractorStrategy,
            partitionSelectorStrategy);
    }

    public int PartitionCount
    {
        get { return _partitionHandler.PartitionCount; }
        set { _partitionHandler.PartitionCount = value; }
    }

    public override IMessage PreSend(IMessage message, IMessageChannel channel)
    {
        var objMessage = message as IMessage<object> ?? Message.Create(message.Payload, message.Headers); // Primitives are not covariant with out T, so box the primitive ...

        if (!message.Headers.ContainsKey(BinderHeaders.PARTITION_OVERRIDE))
        {
            var partition = _partitionHandler.DeterminePartition(message);
            return _messageBuilderFactory
                .FromMessage(objMessage)
                .SetHeader(BinderHeaders.PARTITION_HEADER, partition).Build();
        }
        else
        {
            return _messageBuilderFactory
                .FromMessage(objMessage)
                .SetHeader(BinderHeaders.PARTITION_HEADER, message.Headers[BinderHeaders.PARTITION_OVERRIDE])
                .RemoveHeader(BinderHeaders.PARTITION_OVERRIDE).Build();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.Binder
{
    public class PartitionHandler
    {
        internal readonly IPartitionKeyExtractorStrategy _partitionKeyExtractorStrategy;
        internal readonly IPartitionSelectorStrategy _partitionSelectorStrategy;

        private readonly IProducerOptions _producerOptions;

        private IExpressionParser _expressionParser;
        private IEvaluationContext _evaluationContext;

        public PartitionHandler(
                IExpressionParser expressionParser,
                IEvaluationContext evaluationContext,
                IProducerOptions options,
                IPartitionKeyExtractorStrategy partitionKeyExtractorStrategy,
                IPartitionSelectorStrategy partitionSelectorStrategy)
        {
            _expressionParser = expressionParser;
            _evaluationContext = evaluationContext ?? new StandardEvaluationContext();
            _producerOptions = options;
            _partitionKeyExtractorStrategy = partitionKeyExtractorStrategy;
            _partitionSelectorStrategy = partitionSelectorStrategy;
            PartitionCount = _producerOptions.PartitionCount;
        }

        public int PartitionCount { get; set; }

        public int DeterminePartition(IMessage message)
        {
            var key = ExtractKey(message);

            int partition;
            if (!string.IsNullOrEmpty(_producerOptions.PartitionSelectorExpression) && _expressionParser != null)
            {
                var expr = _expressionParser.ParseExpression(_producerOptions.PartitionSelectorExpression);
                partition = expr.GetValue<int>(_evaluationContext, key);
            }
            else
            {
                partition = _partitionSelectorStrategy.SelectPartition(key, PartitionCount);
            }

            //// protection in case a user selector returns a negative.
            return Math.Abs(partition % PartitionCount);
        }

        private object ExtractKey(IMessage message)
        {
            var key = InvokeKeyExtractor(message);
            if (key == null && !string.IsNullOrEmpty(_producerOptions.PartitionKeyExpression) && _expressionParser != null)
            {
                var expr = _expressionParser.ParseExpression(_producerOptions.PartitionKeyExpression);
                key = expr.GetValue(_evaluationContext ?? new StandardEvaluationContext(), message);
            }

            if (key == null)
            {
                throw new ArgumentException("Partition key cannot be null");
            }

            return key;
        }

        private object InvokeKeyExtractor(IMessage message)
        {
            if (_partitionKeyExtractorStrategy != null)
            {
                return _partitionKeyExtractorStrategy.ExtractKey(message);
            }

            return null;
        }
    }
}

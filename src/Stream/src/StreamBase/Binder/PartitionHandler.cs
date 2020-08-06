// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression;
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
        private readonly IApplicationContext _applicationContext;

        private IExpressionParser _expressionParser;
        private IEvaluationContext _evaluationContext;

        public PartitionHandler(
                IApplicationContext applicationContext,
                IProducerOptions options,
                IPartitionKeyExtractorStrategy partitionKeyExtractorStrategy,
                IPartitionSelectorStrategy partitionSelectorStrategy)
        {
            _applicationContext = applicationContext;
            _producerOptions = options;
            _partitionKeyExtractorStrategy = partitionKeyExtractorStrategy;
            _partitionSelectorStrategy = partitionSelectorStrategy;
            PartitionCount = _producerOptions.PartitionCount;
        }

        public int PartitionCount { get; set; }

        public IExpressionParser ExpressionParser
        {
            get
            {
                if (_expressionParser == null)
                {
                    _expressionParser = IntegrationContextUtils.GetExpressionParser(_applicationContext);
                }

                return _expressionParser;
            }

            set
            {
                _expressionParser = value;
            }
        }

        public IEvaluationContext EvaluationContext
        {
            get
            {
                if (_evaluationContext == null)
                {
                    _evaluationContext = IntegrationContextUtils.GetEvaluationContext(_applicationContext);
                }

                return _evaluationContext;
            }

            set
            {
                _evaluationContext = value;
            }
        }

        public int DeterminePartition(IMessage message)
        {
            var key = ExtractKey(message);

            int partition;
            if (!string.IsNullOrEmpty(_producerOptions.PartitionSelectorExpression) && ExpressionParser != null)
            {
                var expr = ExpressionParser.ParseExpression(_producerOptions.PartitionSelectorExpression);
                partition = expr.GetValue<int>(EvaluationContext, key);
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
            if (key == null && !string.IsNullOrEmpty(_producerOptions.PartitionKeyExpression) && ExpressionParser != null)
            {
                var expr = ExpressionParser.ParseExpression(_producerOptions.PartitionKeyExpression);
                key = expr.GetValue(EvaluationContext, message);
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

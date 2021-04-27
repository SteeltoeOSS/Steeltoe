// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitExpressionEvaluatingInterceptor : IChannelInterceptor
    {
        public const string ROUTING_KEY_HEADER = "scst_routingKey";
        public const string DELAY_HEADER = "scst_delay";

        public int Order => 0;

        private IExpressionParser Parser { get; } = new SpelExpressionParser();

        private IExpression RoutingKeyExpression { get; }

        private IExpression DelayExpression { get; }

        private IEvaluationContext EvaluationContext { get; }

        public RabbitExpressionEvaluatingInterceptor(IExpression routingKeyExpression, IExpression delayExpression, IEvaluationContext evaluationContext)
        {
            if (routingKeyExpression == null && delayExpression == null)
            {
                throw new ArgumentException("At least one expression is required");
            }

            if (evaluationContext == null)
            {
                throw new ArgumentNullException(nameof(evaluationContext));
            }

            if (routingKeyExpression != null)
            {
                RoutingKeyExpression = routingKeyExpression;
            }
            else
            {
                RoutingKeyExpression = null;
            }

            if (delayExpression != null)
            {
                DelayExpression = delayExpression;
            }
            else
            {
                DelayExpression = null;
            }

            EvaluationContext = evaluationContext;
        }

        public void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception exception)
        {
            // Do nothing
        }

        public void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            // Do nothing
        }

        public IMessage PostReceive(IMessage message, IMessageChannel channel)
        {
            return message;
        }

        public void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            // Do nothing
        }

        public bool PreReceive(IMessageChannel channel)
        {
            return true;
        }

        public IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            var builder = IntegrationMessageBuilder.FromMessage(message);
            if (RoutingKeyExpression != null)
            {
                builder.SetHeader(ROUTING_KEY_HEADER, RoutingKeyExpression.GetValue(EvaluationContext, message));
            }

            if (DelayExpression != null)
            {
                builder.SetHeader(DELAY_HEADER, DelayExpression.GetValue(EvaluationContext, message));
            }

            return builder.Build();
        }
    }
}

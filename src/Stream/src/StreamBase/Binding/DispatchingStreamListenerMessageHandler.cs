// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    public class DispatchingStreamListenerMessageHandler : AbstractReplyProducingMessageHandler
    {
        private readonly List<ConditionalStreamListenerMessageHandlerWrapper> _handlerMethods;
        private readonly bool _evaluateExpressions;
        private readonly IEvaluationContext _evaluationContext;

        internal DispatchingStreamListenerMessageHandler(
            IApplicationContext context,
            ICollection<ConditionalStreamListenerMessageHandlerWrapper> handlerMethods,
            IEvaluationContext evaluationContext = null)
            : base(context)
        {
            if (handlerMethods == null || handlerMethods.Count == 0)
            {
                throw new ArgumentException(nameof(handlerMethods));
            }

            _handlerMethods = new List<ConditionalStreamListenerMessageHandlerWrapper>(handlerMethods);
            var evaluateExpressions = false;
            foreach (var handlerMethod in handlerMethods)
            {
                if (handlerMethod.Condition != null)
                {
                    evaluateExpressions = true;
                    break;
                }
            }

            _evaluateExpressions = evaluateExpressions;
            if (evaluateExpressions && evaluationContext == null)
            {
                throw new ArgumentNullException(nameof(evaluationContext));
            }

            _evaluationContext = evaluationContext;
        }

        protected override bool ShouldCopyRequestHeaders
        {
            get { return false; }
        }

        public override void Initialize()
        {
            // Nothing to do
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            var matchingHandlers = _evaluateExpressions
                    ? FindMatchingHandlers(requestMessage) : _handlerMethods;
            if (matchingHandlers.Count == 0)
            {
                return null;
            }
            else if (matchingHandlers.Count > 1)
            {
                foreach (var matchingMethod in matchingHandlers)
                {
                    matchingMethod.StreamListenerMessageHandler.HandleMessage(requestMessage);
                }

                return null;
            }
            else
            {
                var singleMatchingHandler = matchingHandlers[0];
                singleMatchingHandler.StreamListenerMessageHandler.HandleMessage(requestMessage);
                return null;
            }
        }

        private List<ConditionalStreamListenerMessageHandlerWrapper> FindMatchingHandlers(IMessage message)
        {
            var matchingMethods = new List<ConditionalStreamListenerMessageHandlerWrapper>();
            foreach (var wrapper in _handlerMethods)
            {
                if (wrapper.Condition == null)
                {
                    matchingMethods.Add(wrapper);
                }
                else
                {
                    var conditionMetOnMessage = wrapper.Condition.GetValue<bool>(_evaluationContext, message);
                    if (conditionMetOnMessage)
                    {
                        matchingMethods.Add(wrapper);
                    }
                }
            }

            return matchingMethods;
        }

        internal class ConditionalStreamListenerMessageHandlerWrapper
        {
            private readonly IExpression _condition;

            private readonly StreamListenerMessageHandler _streamListenerMessageHandler;

            internal ConditionalStreamListenerMessageHandlerWrapper(IExpression condition, StreamListenerMessageHandler streamListenerMessageHandler)
            {
                if (streamListenerMessageHandler == null)
                {
                    throw new ArgumentNullException(nameof(streamListenerMessageHandler));
                }

                if (!(condition == null || streamListenerMessageHandler.IsVoid))
                {
                    throw new ArgumentException("cannot specify a condition and a return value at the same time");
                }

                _condition = condition;
                _streamListenerMessageHandler = streamListenerMessageHandler;
            }

            public IExpression Condition
            {
                get { return _condition; }
            }

            public bool IsVoid
            {
                get { return _streamListenerMessageHandler.IsVoid; }
            }

            public StreamListenerMessageHandler StreamListenerMessageHandler
            {
                get { return _streamListenerMessageHandler; }
            }
        }
    }
}

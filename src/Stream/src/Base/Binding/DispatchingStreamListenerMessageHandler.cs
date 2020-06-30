// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression;
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
            private readonly IExpression condition;

            private readonly StreamListenerMessageHandler streamListenerMessageHandler;

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

                this.condition = condition;
                this.streamListenerMessageHandler = streamListenerMessageHandler;
            }

            public IExpression Condition
            {
                get { return condition; }
            }

            public bool IsVoid
            {
                get { return streamListenerMessageHandler.IsVoid; }
            }

            public StreamListenerMessageHandler StreamListenerMessageHandler
            {
                get { return streamListenerMessageHandler; }
            }
        }
    }
}

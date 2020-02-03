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

using Steeltoe.Common.Expression;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Integration.Handler.Support
{
    public class PayloadsArgumentResolver : AbstractExpressionEvaluator, IHandlerMethodArgumentResolver
    {
        private readonly Dictionary<ParameterInfo, IExpression> _expressionCache = new Dictionary<ParameterInfo, IExpression>();

        public PayloadsArgumentResolver(IExpressionParser expressionParser, IEvaluationContext evaluationContext)
            : base(expressionParser, evaluationContext)
        {
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<PayloadsAttribute>() != null;
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var payload = message.Payload;
            if (!(payload is ICollection<IMessage>))
            {
                throw new ArgumentException("This Argument Resolver support only messages with payload as ICollection<IMessage>");
            }

            var messages = (ICollection<IMessage>)payload;

            if (!_expressionCache.ContainsKey(parameter))
            {
                var payloads = parameter.GetCustomAttribute<PayloadsAttribute>();
                if (!string.IsNullOrEmpty(payloads.Expression))
                {
                    _expressionCache.Add(parameter, ExpressionParser.ParseExpression("![payload." + payloads.Expression + "]"));
                }
                else
                {
                    _expressionCache.Add(parameter, null);
                }
            }

            _expressionCache.TryGetValue(parameter, out var expression);
            if (expression != null)
            {
                return EvaluateExpression(expression, messages, parameter.ParameterType);
            }
            else
            {
                var payloads = messages.Select((m) => m.Payload).ToList();
                return EvaluationContext.TypeConverter.ConvertValue(payloads, payloads.GetType(), parameter.ParameterType);
            }
        }
    }
}

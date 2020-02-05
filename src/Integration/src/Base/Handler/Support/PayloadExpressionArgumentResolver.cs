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
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Integration.Handler.Support
{
    public class PayloadExpressionArgumentResolver : AbstractExpressionEvaluator, IHandlerMethodArgumentResolver
    {
        private readonly Dictionary<ParameterInfo, IExpression> _expressionCache = new Dictionary<ParameterInfo, IExpression>();

        public PayloadExpressionArgumentResolver(IExpressionParser expressionParser, IEvaluationContext evaluationContext)
            : base(expressionParser, evaluationContext)
        {
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            var ann = parameter.GetCustomAttribute<PayloadAttribute>();
            return ann != null && !string.IsNullOrEmpty(ann.Expression);
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            _expressionCache.TryGetValue(parameter, out IExpression expression);
            if (expression == null)
            {
                var ann = parameter.GetCustomAttribute<PayloadAttribute>();
                expression = ExpressionParser.ParseExpression(ann.Expression);
                _expressionCache.Add(parameter, expression);
            }

            return EvaluateExpression(expression, message.Payload, parameter.ParameterType);
        }
    }
}

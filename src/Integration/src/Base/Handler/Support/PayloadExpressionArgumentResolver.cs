﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

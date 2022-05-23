// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
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
        private readonly Dictionary<ParameterInfo, IExpression> _expressionCache = new ();

        public PayloadsArgumentResolver(IApplicationContext context)
            : base(context)
        {
        }

        public bool SupportsParameter(ParameterInfo parameter) => parameter.GetCustomAttribute<PayloadsAttribute>() != null;

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var payload = message.Payload;
            if (payload is not ICollection<IMessage> messages)
            {
                throw new ArgumentException("This Argument Resolver support only messages with payload as ICollection<IMessage>");
            }

            if (!_expressionCache.ContainsKey(parameter))
            {
                var payloads = parameter.GetCustomAttribute<PayloadsAttribute>();

                var value = !string.IsNullOrEmpty(payloads.Expression) ? ExpressionParser.ParseExpression($"![payload.{payloads.Expression}]") : null;
                _expressionCache.Add(parameter, value);
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

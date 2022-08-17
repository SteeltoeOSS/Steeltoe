// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Integration.Handler.Support;

public class PayloadsArgumentResolver : AbstractExpressionEvaluator, IHandlerMethodArgumentResolver
{
    private readonly Dictionary<ParameterInfo, IExpression> _expressionCache = new();

    public PayloadsArgumentResolver(IApplicationContext context)
        : base(context)
    {
    }

    public bool SupportsParameter(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<PayloadsAttribute>() != null;
    }

    public object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        object payload = message.Payload;

        if (payload is not ICollection<IMessage> messages)
        {
            throw new ArgumentException("This Argument Resolver supports only messages with payload of type ICollection<IMessage>.", nameof(message));
        }

        if (!_expressionCache.ContainsKey(parameter))
        {
            var attribute = parameter.GetCustomAttribute<PayloadsAttribute>();

            IExpression value = !string.IsNullOrEmpty(attribute.Expression) ? ExpressionParser.ParseExpression($"![payload.{attribute.Expression}]") : null;
            _expressionCache.Add(parameter, value);
        }

        _expressionCache.TryGetValue(parameter, out IExpression expression);

        if (expression != null)
        {
            return EvaluateExpression(expression, messages, parameter.ParameterType);
        }

        List<object> payloads = messages.Select(m => m.Payload).ToList();
        return EvaluationContext.TypeConverter.ConvertValue(payloads, payloads.GetType(), parameter.ParameterType);
    }
}

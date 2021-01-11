// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections;
using System.Reflection;

namespace Steeltoe.Integration.Handler.Support
{
    public class DictionaryArgumentResolver : AbstractExpressionEvaluator, IHandlerMethodArgumentResolver
    {
        public DictionaryArgumentResolver(IApplicationContext context)
            : base(context)
        {
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var payload = message.Payload;
            if (parameter.GetCustomAttribute<HeadersAttribute>() == null && payload is IDictionary)
            {
                return payload;
            }
            else
            {
                return message.Headers;
            }
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<PayloadAttribute>() == null &&
                typeof(IDictionary).IsAssignableFrom(parameter.ParameterType);
        }
    }
}

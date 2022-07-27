// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class HeadersMethodArgumentResolver : IHandlerMethodArgumentResolver
{
    public object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        var paramType = parameter.ParameterType;
        if (typeof(IDictionary<string, object>).IsAssignableFrom(paramType))
        {
            return message.Headers;
        }
        else if (typeof(MessageHeaderAccessor) == paramType)
        {
            var accessor = MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor));
            return accessor ?? new MessageHeaderAccessor(message);
        }
        else if (typeof(MessageHeaderAccessor).IsAssignableFrom(paramType))
        {
            var accessor = MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor));
            if (accessor != null && paramType.IsInstanceOfType(accessor))
            {
                return accessor;
            }
            else
            {
                var method = ReflectionHelpers.FindMethod(paramType, "Wrap", new Type[] { typeof(IMessage) });
                if (method == null)
                {
                    throw new InvalidOperationException("Cannot create accessor of type " + paramType + " for message " + message);
                }

                return ReflectionHelpers.Invoke(method, null, new object[] { message });
            }
        }
        else
        {
            throw new InvalidOperationException("Unexpected parameter of type " + paramType + " in method " + parameter.Member + ". ");
        }
    }

    public bool SupportsParameter(ParameterInfo parameter)
    {
        var paramType = parameter.ParameterType;
        return (parameter.GetCustomAttribute<HeadersAttribute>() != null && typeof(IDictionary<string, object>).IsAssignableFrom(paramType)) ||
               typeof(IMessageHeaders).IsAssignableFrom(paramType) || typeof(IMessageHeaderAccessor).IsAssignableFrom(paramType);
    }
}
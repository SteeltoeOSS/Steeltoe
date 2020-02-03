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

using Steeltoe.Common.Reflection;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
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
                var accessor = MessageHeaderAccessor.GetAccessor<MessageHeaderAccessor>(message, typeof(MessageHeaderAccessor));
                return accessor != null ? accessor : new MessageHeaderAccessor(message);
            }
            else if (typeof(MessageHeaderAccessor).IsAssignableFrom(paramType))
            {
                var accessor = MessageHeaderAccessor.GetAccessor<MessageHeaderAccessor>(message, typeof(MessageHeaderAccessor));
                if (accessor != null && paramType.IsAssignableFrom(accessor.GetType()))
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class AttributeExceptionHandlerMethodResolver : AbstractExceptionHandlerMethodResolver
    {
        public AttributeExceptionHandlerMethodResolver(Type handlerType)
        : base(InitExceptionMappings(handlerType))
        {
        }

        private static Dictionary<Type, MethodInfo> InitExceptionMappings(Type handlerType)
        {
            var methods = new Dictionary<MethodInfo, MessageExceptionHandlerAttribute>();
            var targets = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in targets)
            {
                var attribute = method.GetCustomAttribute<MessageExceptionHandlerAttribute>();
                if (attribute != null)
                {
                    methods.Add(method, attribute);
                }
            }

            var result = new Dictionary<Type, MethodInfo>();
            foreach (var entry in methods)
            {
                var method = entry.Key;
                var exceptionTypes = new List<Type>(entry.Value.Exceptions);
                if (exceptionTypes.Count == 0)
                {
                    exceptionTypes.AddRange(GetExceptionsFromMethodSignature(method));
                }

                foreach (var exceptionType in exceptionTypes)
                {
                    result.TryGetValue(exceptionType, out var oldMethod);
                    result[exceptionType] = method;

                    if (oldMethod != null && !oldMethod.Equals(method))
                    {
                        throw new InvalidOperationException("Ambiguous @ExceptionHandler method mapped for [" +
                                exceptionType + "]: {" + oldMethod + ", " + method + "}");
                    }
                }
            }

            return result;
        }
    }
}

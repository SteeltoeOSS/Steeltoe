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
            var targets = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);  // TODO: Static supported?
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

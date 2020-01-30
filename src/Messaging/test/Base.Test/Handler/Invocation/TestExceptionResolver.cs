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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation.Test
{
    internal class TestExceptionResolver : AbstractExceptionHandlerMethodResolver
    {
        public TestExceptionResolver(Type handlerType)
        : base(InitExceptionMappings(handlerType))
        {
        }

        private static IDictionary<Type, MethodInfo> InitExceptionMappings(Type handlerType)
        {
            IDictionary<Type, MethodInfo> result = new Dictionary<Type, MethodInfo>();

            foreach (var method in GetExceptionHandlerMethods(handlerType))
            {
                foreach (var exception in GetExceptionsFromMethodSignature(method))
                {
                    result.Add(exception, method);
                }
            }

            return result;
        }

        private static IEnumerable<MethodInfo> GetExceptionHandlerMethods(Type handlerType)
        {
            List<MethodInfo> results = new List<MethodInfo>();
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name.StartsWith("Handle") && method.Name.EndsWith("Exception"))
                {
                    results.Add(method);
                }
            }

            return results;
        }
    }
}

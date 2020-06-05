// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var results = new List<MethodInfo>();
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

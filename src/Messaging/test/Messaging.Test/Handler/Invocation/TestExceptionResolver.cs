// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Messaging.Test.Handler.Invocation;

internal sealed class TestExceptionResolver : AbstractExceptionHandlerMethodResolver
{
    public TestExceptionResolver(Type handlerType)
        : base(InitExceptionMappings(handlerType))
    {
    }

    private static IDictionary<Type, MethodInfo> InitExceptionMappings(Type handlerType)
    {
        IDictionary<Type, MethodInfo> result = new Dictionary<Type, MethodInfo>();

        foreach (MethodInfo method in GetExceptionHandlerMethods(handlerType))
        {
            foreach (Type exception in GetExceptionsFromMethodSignature(method))
            {
                result.Add(exception, method);
            }
        }

        return result;
    }

    private static IEnumerable<MethodInfo> GetExceptionHandlerMethods(Type handlerType)
    {
        var results = new List<MethodInfo>();
        MethodInfo[] methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        foreach (MethodInfo method in methods)
        {
            if (method.Name.StartsWith("Handle", StringComparison.Ordinal) && method.Name.EndsWith("Exception", StringComparison.Ordinal))
            {
                results.Add(method);
            }
        }

        return results;
    }
}

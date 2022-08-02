// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Expression.Internal;

namespace Steeltoe.Integration.Util;

public class AnnotatedMethodFilter : IMethodFilter
{
    private readonly Type _annotationType;

    private readonly string _methodName;

    private readonly bool _requiresReply;

    public AnnotatedMethodFilter(Type annotationType, string methodName, bool requiresReply)
    {
        _annotationType = annotationType;
        _methodName = methodName;
        _requiresReply = requiresReply;
    }

    public List<MethodInfo> Filter(List<MethodInfo> methods)
    {
        var annotatedCandidates = new List<MethodInfo>();
        var fallbackCandidates = new List<MethodInfo>();

        foreach (MethodInfo method in methods)
        {
            if (_requiresReply && (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task)))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(_methodName) && !_methodName.Equals(method.Name))
            {
                continue;
            }

            if (_annotationType != null && method.GetCustomAttribute(_annotationType) != null)
            {
                annotatedCandidates.Add(method);
            }
            else
            {
                fallbackCandidates.Add(method);
            }
        }

        if (annotatedCandidates.Count > 0)
        {
            return annotatedCandidates;
        }

        return fallbackCandidates;
    }
}

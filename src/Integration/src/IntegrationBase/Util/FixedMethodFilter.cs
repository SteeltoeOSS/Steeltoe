// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Expression.Internal;

namespace Steeltoe.Integration.Util;

public class FixedMethodFilter : IMethodFilter
{
    private readonly MethodInfo _method;

    public FixedMethodFilter(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        _method = method;
    }

    public List<MethodInfo> Filter(List<MethodInfo> methods)
    {
        if (methods != null && methods.Contains(_method))
        {
            return new List<MethodInfo>
            {
                _method
            };
        }

        return new List<MethodInfo>();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public static class MethodBaseExtensions
{
    public static bool IsVarArgs(this MethodBase method)
    {
        if (method == null)
        {
            return false;
        }

        ParameterInfo[] parameters = method.GetParameters();

        if (parameters.Length == 0)
        {
            return false;
        }

        ParameterInfo lastParam = parameters[parameters.Length - 1];
        return lastParam.GetCustomAttribute<ParamArrayAttribute>() != null;
    }

    public static Type[] GetParameterTypes(this MethodBase method)
    {
        ParameterInfo[] param = method.GetParameters();
        var result = new Type[param.Length];

        for (int i = 0; i < param.Length; i++)
        {
            result[i] = param[i].ParameterType;
        }

        return result;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.Common.Util;

public static class ClassUtils
{
    public static Type GetGenericTypeDefinition(Type type)
    {
        if (type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            return type.GetGenericTypeDefinition();
        }

        return type;
    }

    public static bool IsAssignableValue(Type type, object value)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return value != null ? IsAssignable(type, value.GetType()) : !type.IsPrimitive;
    }

    public static bool IsAssignable(Type lhsType, Type rhsType)
    {
        if (lhsType == null)
        {
            throw new ArgumentNullException(nameof(lhsType));
        }

        if (rhsType == null)
        {
            throw new ArgumentNullException(nameof(rhsType));
        }

        if (lhsType.IsAssignableFrom(rhsType))
        {
            return true;
        }

        // if (lhsType.IsPrimitive)
        // {
        //    Type resolvedPrimitive = primitiveWrapperTypeMap.get(rhsType);
        //    if (lhsType == resolvedPrimitive)
        //    {
        //        return true;
        //    }
        // }
        // else
        // {
        //    Type resolvedWrapper = primitiveTypeToWrapperMap.get(rhsType);
        //    if (resolvedWrapper != null && lhsType.isAssignableFrom(resolvedWrapper))
        //    {
        //        return true;
        //    }
        // }
        return false;
    }

    public static MethodInfo GetInterfaceMethodIfPossible(MethodInfo method)
    {
        if (!method.IsPublic || method.DeclaringType.IsInterface)
        {
            return method;
        }

        var current = method.DeclaringType;
        while (current != null && current != typeof(object))
        {
            var interfaces = current.GetInterfaces();
            foreach (var ifc in interfaces)
            {
                try
                {
                    var found = ifc.GetMethod(method.Name, GetParameterTypes(method));
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            current = current.BaseType;
        }

        return method;
    }

    public static string GetQualifiedMethodName(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        return $"{method.DeclaringType.FullName}.{method.Name}";
    }

    public static Type[] GetParameterTypes(MethodBase method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var results = new Type[method.GetParameters().Length];
        var index = 0;
        foreach (var param in method.GetParameters())
        {
            results[index++] = param.ParameterType;
        }

        return results;
    }

    public static object[][] GetParameterAttributes(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var paramsAttributes = new object[parameters.Length][];
        for (var i = 0; i < parameters.Length; i++)
        {
            paramsAttributes[i] = parameters[i].GetCustomAttributes(false);
        }

        return paramsAttributes;
    }

    public static Type DetermineCommonAncestor(Type clazz1, Type clazz2)
    {
        if (clazz1 == null)
        {
            return clazz2;
        }

        if (clazz2 == null)
        {
            return clazz1;
        }

        if (clazz1.IsAssignableFrom(clazz2))
        {
            return clazz1;
        }

        if (clazz2.IsAssignableFrom(clazz1))
        {
            return clazz2;
        }

        var ancestor = clazz1;
        do
        {
            ancestor = ancestor.BaseType;
            if (ancestor == null || typeof(object) == ancestor)
            {
                return null;
            }
        }
        while (!ancestor.IsAssignableFrom(clazz2));
        return ancestor;
    }
}

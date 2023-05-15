// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;
using Steeltoe.Common;

namespace Steeltoe.Connector.RuntimeTypeAccess;

/// <summary>
/// Base type for reflection-based type/member access.
/// </summary>
internal abstract class ReflectionAccessor
{
    private readonly Type _type;

    protected ReflectionAccessor(Type type)
    {
        ArgumentGuard.NotNull(type);

        _type = type;
    }

    protected T GetPropertyValue<T>(string name, object? instance)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        object? value = GetPropertyValue(name, instance);
        return (T)value!;
    }

    private object? GetPropertyValue(string name, object? instance)
    {
        MethodInfo propertySetter = GetPropertyGetter(name);
        return propertySetter.Invoke(instance, Array.Empty<object>());
    }

    private MethodInfo GetPropertyGetter(string name)
    {
        PropertyInfo propertyInfo = GetProperty(name);

        if (propertyInfo.GetMethod == null)
        {
            throw new InvalidOperationException($"Property '{_type}.{name}' is write-only.");
        }

        return propertyInfo.GetMethod;
    }

    protected void SetPropertyValue(string name, object? instance, object? value)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        MethodInfo propertySetter = GetPropertySetter(name);

        propertySetter.Invoke(instance, new[]
        {
            value
        });
    }

    private MethodInfo GetPropertySetter(string name)
    {
        PropertyInfo propertyInfo = GetProperty(name);

        if (propertyInfo.SetMethod == null)
        {
            throw new InvalidOperationException($"Property '{_type}.{name}' is read-only.");
        }

        return propertyInfo.SetMethod;
    }

    private PropertyInfo GetProperty(string name)
    {
        PropertyInfo? propertyInfo = _type.GetProperty(name);

        if (propertyInfo == null)
        {
            throw new InvalidOperationException($"Property '{_type}.{name}' does not exist.");
        }

        return propertyInfo;
    }

    protected object? InvokeMethodOverload(string name, Type[] parameterTypes, object? instance, BindingFlags? bindingFlags, object?[] arguments)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(parameterTypes);
        ArgumentGuard.ElementsNotNull(parameterTypes);
        ArgumentGuard.NotNull(arguments);

        MethodInfo methodInfo = GetMethod(name, instance == null, parameterTypes, bindingFlags);
        return methodInfo.Invoke(instance, arguments);
    }

    protected object? InvokeMethod(string name, object? instance, BindingFlags? bindingFlags, object?[] arguments)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(arguments);

        MethodInfo methodInfo = GetMethod(name, instance == null, null, bindingFlags);
        return methodInfo.Invoke(instance, arguments);
    }

    private MethodInfo GetMethod(string name, bool isStatic, Type[]? parameterTypes, BindingFlags? bindingFlags)
    {
        bindingFlags ??= BindingFlags.Public;
        bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;

        MethodInfo? methodInfo = parameterTypes == null ? _type.GetMethod(name, bindingFlags.Value) : _type.GetMethod(name, bindingFlags.Value, parameterTypes);

        if (methodInfo == null)
        {
            throw new InvalidOperationException($"Method '{_type}.{name}' does not exist.");
        }

        return methodInfo;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;

namespace Steeltoe.Common.DynamicTypeAccess;

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

    protected T GetPrivateFieldValue<T>(string name, object? instance)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        object? value = GetPrivateFieldValue(name, instance);
        return (T)value!;
    }

    private object? GetPrivateFieldValue(string name, object? instance)
    {
        FieldInfo fieldInfo = GetField(name, false, instance == null);
        return fieldInfo.GetValue(instance);
    }

    private FieldInfo GetField(string name, bool isPublic, bool isStatic)
    {
        BindingFlags bindingFlags = CreateBindingFlags(isPublic, isStatic);
        FieldInfo? fieldInfo = _type.GetField(name, bindingFlags);

        if (fieldInfo == null)
        {
            throw new InvalidOperationException($"Field '{_type}.{name}' does not exist.");
        }

        return fieldInfo;
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

    protected object? InvokeMethod(string name, bool isPublic, object? instance, object?[] arguments)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(arguments);

        MethodInfo methodInfo = GetMethod(name, isPublic, instance == null, null);
        return methodInfo.Invoke(instance, arguments);
    }

    protected object? InvokeMethodOverload(string name, bool isPublic, Type[] parameterTypes, object? instance, object?[] arguments)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(parameterTypes);
        ArgumentGuard.ElementsNotNull(parameterTypes);
        ArgumentGuard.NotNull(arguments);

        MethodInfo methodInfo = GetMethod(name, isPublic, instance == null, parameterTypes);
        return methodInfo.Invoke(instance, arguments);
    }

    private MethodInfo GetMethod(string name, bool isPublic, bool isStatic, Type[]? parameterTypes)
    {
        BindingFlags bindingFlags = CreateBindingFlags(isPublic, isStatic);

        MethodInfo? methodInfo = parameterTypes == null ? _type.GetMethod(name, bindingFlags) : _type.GetMethod(name, bindingFlags, parameterTypes);

        if (methodInfo == null)
        {
            throw new InvalidOperationException($"Method '{_type}.{name}' does not exist.");
        }

        return methodInfo;
    }

    private static BindingFlags CreateBindingFlags(bool isPublic, bool isStatic)
    {
        BindingFlags bindingFlags = default;
        bindingFlags |= isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
        bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;
        return bindingFlags;
    }
}

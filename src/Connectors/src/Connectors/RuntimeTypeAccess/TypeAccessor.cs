// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Connectors.RuntimeTypeAccess;

/// <summary>
/// Provides reflection-based access to static members of a <see cref="System.Type" />.
/// </summary>
internal sealed class TypeAccessor : ReflectionAccessor
{
    public Type Type { get; }

    public TypeAccessor(Type type)
        : base(type)
    {
        Type = type;
    }

    public InstanceAccessor CreateInstance(params object?[]? arguments)
    {
        object instance = arguments is { Length: > 0 } ? Activator.CreateInstance(Type, arguments)! : Activator.CreateInstance(Type)!;
        return new InstanceAccessor(this, instance);
    }

    public T GetPropertyValue<T>(string name)
    {
        return GetPropertyValue<T>(name, null);
    }

    public void SetPropertyValue(string name, object? value)
    {
        SetPropertyValue(name, null, value);
    }

    public object? InvokeMethod(string name, bool isPublic, params object?[] arguments)
    {
        return base.InvokeMethod(name, isPublic, null, arguments);
    }

    public object? InvokeMethodOverload(string name, bool isPublic, Type[] parameterTypes, params object?[] arguments)
    {
        return base.InvokeMethodOverload(name, isPublic, parameterTypes, null, arguments);
    }
}

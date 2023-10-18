// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.DynamicTypeAccess;

/// <summary>
/// Provides reflection-based access to the members of an object instance.
/// </summary>
internal sealed class InstanceAccessor : ReflectionAccessor
{
    public TypeAccessor DeclaredTypeAccessor { get; }
    public object Instance { get; }

    public InstanceAccessor(TypeAccessor declaredTypeAccessor, object instance)
        : base(AssertNotNull(declaredTypeAccessor))
    {
        ArgumentGuard.NotNull(instance);

        if (!declaredTypeAccessor.Type.IsInstanceOfType(instance))
        {
            throw new InvalidOperationException($"Object of type '{instance.GetType()}' is not assignable to type '{declaredTypeAccessor.Type}'.");
        }

        DeclaredTypeAccessor = declaredTypeAccessor;
        Instance = instance;
    }

    private static Type AssertNotNull(TypeAccessor declaredTypeAccessor)
    {
        ArgumentGuard.NotNull(declaredTypeAccessor);

        return declaredTypeAccessor.Type;
    }

    public InstanceAccessor AsRuntimeType()
    {
        // Interface members that are defined in a base interface aren't found by reflection.
        // To workaround that, reflect against the actual implementation.

        Type runtimeType = Instance.GetType();
        var typeAccessor = new TypeAccessor(runtimeType);
        return new InstanceAccessor(typeAccessor, Instance);
    }

    public T GetPropertyValue<T>(string name)
    {
        return GetPropertyValue<T>(name, Instance);
    }

    public void SetPropertyValue(string name, object? value)
    {
        SetPropertyValue(name, Instance, value);
    }

    public object? InvokeMethod(string name, bool isPublic, params object?[] arguments)
    {
        return base.InvokeMethod(name, isPublic, Instance, arguments);
    }

    public object? InvokeMethodOverload(string name, bool isPublic, Type[] parameterTypes, params object?[] arguments)
    {
        return base.InvokeMethodOverload(name, isPublic, parameterTypes, Instance, arguments);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class DataBindingPropertyAccessor : ReflectivePropertyAccessor
{
    private DataBindingPropertyAccessor(bool allowWrite)
        : base(allowWrite)
    {
    }

    public static DataBindingPropertyAccessor ForReadOnlyAccess()
    {
        return new DataBindingPropertyAccessor(false);
    }

    public static DataBindingPropertyAccessor ForReadWriteAccess()
    {
        return new DataBindingPropertyAccessor(true);
    }

    protected override bool IsCandidateForProperty(MethodInfo method, Type targetClass)
    {
        Type clazz = method.DeclaringType;
        return clazz != typeof(object) && clazz != typeof(Type);
    }
}

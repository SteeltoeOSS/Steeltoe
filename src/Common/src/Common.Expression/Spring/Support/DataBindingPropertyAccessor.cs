﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
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
        var clazz = method.DeclaringType;
        return clazz != typeof(object) && clazz != typeof(Type);
    }
}
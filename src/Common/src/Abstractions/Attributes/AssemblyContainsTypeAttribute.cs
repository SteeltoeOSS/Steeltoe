// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Attributes;

/// <summary>
/// This abstract attribute can be used to quickly identify assemblies containing desired types.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public abstract class AssemblyContainsTypeAttribute : Attribute
{
    public Type ContainedType { get; private set; }

    protected AssemblyContainsTypeAttribute(Type type)
    {
        ContainedType = type;
    }
}

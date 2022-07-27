// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class BinderAttribute : Attribute
{
    public BinderAttribute()
    {
        Name = string.Empty;
        ConfigureClass = string.Empty;
    }

    public BinderAttribute(string name, Type configureClass)
    {
        Name = name;
        ConfigureClass = configureClass.AssemblyQualifiedName;
    }

    public virtual string Name { get; set; }

    public virtual string ConfigureClass { get; set; }
}
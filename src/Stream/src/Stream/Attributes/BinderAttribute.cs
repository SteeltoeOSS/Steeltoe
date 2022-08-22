// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class BinderAttribute : Attribute
{
    public string Name { get; set; }

    public string ConfigureType { get; set; }

    public BinderAttribute()
    {
        Name = string.Empty;
        ConfigureType = string.Empty;
    }

    public BinderAttribute(string name, Type configureType)
    {
        Name = name;
        ConfigureType = configureType.AssemblyQualifiedName;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

public class BinderType : IBinderType
{
    public string Name { get; }

    public string ConfigureClass { get; }

    public string AssemblyPath { get; }

    public BinderType(string name, string configurationClass, string assemblyPath)
    {
        Name = name;
        ConfigureClass = configurationClass;
        AssemblyPath = assemblyPath;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not BinderType other || GetType() != obj.GetType())
        {
            return false;
        }

        return Name == other.Name && ConfigureClass == other.ConfigureClass && AssemblyPath == other.AssemblyPath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, ConfigureClass, AssemblyPath);
    }
}

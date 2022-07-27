// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

public class BinderType : IBinderType
{
    public BinderType(string name, string configurationClass, string assemblyPath)
    {
        Name = name;
        ConfigureClass = configurationClass;
        AssemblyPath = assemblyPath;
    }

    public string Name { get; }

    public string ConfigureClass { get; }

    public string AssemblyPath { get; }

    public override bool Equals(object o)
    {
        if (this == o)
        {
            return true;
        }

        if (o == null || GetType() != o.GetType())
        {
            return false;
        }

        var that = (BinderType)o;
        if (!Name.Equals(that.Name))
        {
            return false;
        }

        return ConfigureClass == that.ConfigureClass &&
               AssemblyPath == that.AssemblyPath;
    }

    public override int GetHashCode()
    {
        var result = Name.GetHashCode();
        result = (31 * result) + ConfigureClass.GetHashCode();

        if (!string.IsNullOrEmpty(AssemblyPath))
        {
            result = (31 * result) + AssemblyPath.GetHashCode();
        }

        return result;
    }
}
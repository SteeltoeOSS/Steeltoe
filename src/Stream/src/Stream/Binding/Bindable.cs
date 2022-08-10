// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Stream.Binding;

public struct Bindable
{
    public bool IsInput { get; set; }

    public string Name { get; set; }

    public Type BindingTargetType { get; set; }

    public MethodInfo FactoryMethod { get; set; }
}

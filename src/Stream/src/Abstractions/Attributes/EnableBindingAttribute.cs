// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class EnableBindingAttribute : Attribute
{
    public EnableBindingAttribute(params Type[] bindings)
    {
        Bindings = bindings;
    }

    public virtual Type[] Bindings { get; set; }
}
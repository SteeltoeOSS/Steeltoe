// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Integration.Config;

public class ServiceActivatorMethod : IServiceActivatorMethod
{
    public ServiceActivatorMethod(MethodInfo method, Type targetClass, ServiceActivatorAttribute attribute)
    {
        Method = method;
        ImplementationType = targetClass;
        Attribute = attribute;
    }

    public MethodInfo Method { get; }

    public ServiceActivatorAttribute Attribute { get; }

    public Type ImplementationType { get; }
}

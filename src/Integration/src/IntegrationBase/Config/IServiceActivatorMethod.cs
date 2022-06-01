// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Integration.Config;

public interface IServiceActivatorMethod
{
    public MethodInfo Method { get; }

    public ServiceActivatorAttribute Attribute { get; }

    public Type ImplementationType { get; }
}

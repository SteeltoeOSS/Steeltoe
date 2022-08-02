// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Config;

public class StreamListenerMethod : IStreamListenerMethod
{
    public MethodInfo Method { get; }

    public StreamListenerAttribute Attribute { get; }

    public Type ImplementationType => Method.DeclaringType;

    public StreamListenerMethod(MethodInfo method, StreamListenerAttribute attribute)
    {
        Method = method;
        Attribute = attribute;
    }
}

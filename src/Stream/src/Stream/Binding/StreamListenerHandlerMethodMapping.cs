// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Stream.Binding;

internal sealed class StreamListenerHandlerMethodMapping
{
    public Type ImplementationType { get; }

    public MethodInfo Method { get; }

    public string Condition { get; }

    public string DefaultOutputChannel { get; }

    public bool CopyHeaders { get; }

    public StreamListenerHandlerMethodMapping(Type implementationType, MethodInfo method, string condition, string defaultOutputChannel, bool copyHeaders)
    {
        ImplementationType = implementationType;
        Method = method;
        Condition = condition;
        DefaultOutputChannel = defaultOutputChannel;
        CopyHeaders = copyHeaders;
    }
}

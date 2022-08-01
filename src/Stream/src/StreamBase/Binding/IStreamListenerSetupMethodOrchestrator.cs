// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Binding;

public interface IStreamListenerSetupMethodOrchestrator
{
    bool Supports(MethodInfo method);

    void OrchestrateStreamListener(StreamListenerAttribute streamListener, MethodInfo method, Type implementationType);

    object[] AdaptAndRetrieveInboundArguments(MethodInfo method, string inboundName, params IStreamListenerParameterAdapter[] streamListenerParameterAdapters);
}

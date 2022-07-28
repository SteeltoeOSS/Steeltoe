// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.Stream.Binding;

public interface IStreamListenerParameterAdapter
{
    bool Supports(Type bindingTargetType, ParameterInfo methodParameter);

    object Adapt(object bindingTarget, ParameterInfo parameter);
}

public interface IStreamListenerParameterAdapter<out TResult, in TTarget> : IStreamListenerParameterAdapter
{
    TResult Adapt(TTarget bindingTarget, ParameterInfo parameter);
}

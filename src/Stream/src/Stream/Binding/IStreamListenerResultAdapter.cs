// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binding;

public interface IStreamListenerResultAdapter
{
    bool Supports(Type resultType, Type bindingTarget);

    IDisposable Adapt(object streamListenerResult, object bindingTarget);
}

public interface IStreamListenerResultAdapter<in TResult, in TTarget> : IStreamListenerResultAdapter
{
    IDisposable Adapt(TResult streamListenerResult, TTarget bindingTarget);
}
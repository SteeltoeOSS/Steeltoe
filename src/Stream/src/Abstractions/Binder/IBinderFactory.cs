// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Binder;

/// <summary>
/// A factory for creating or obtaining binders.
/// </summary>
/// TODO: Figure out Disposable/Closable/DisposableBean usages
public interface IBinderFactory // : IDisposable
{
    /// <summary>
    /// Returns the binder instance associated with the given configuration name. Instance
    /// caching is a requirement, and implementations must return the same instance on
    /// subsequent invocations with the same arguments.
    /// </summary>
    /// <param name="name">the name of the binder in configuration.</param>
    /// <returns>the binder.</returns>
    IBinder GetBinder(string name);

    /// <summary>
    /// Returns the binder instance associated with the given configuration name. Instance
    /// caching is a requirement, and implementations must return the same instance on
    /// subsequent invocations with the same arguments.
    /// </summary>
    /// <param name="name">the name of the binder in configuration.</param>
    /// <param name="bindableType">the binding target type.</param>
    /// <returns>the binder.</returns>
    IBinder GetBinder(string name, Type bindableType);
}

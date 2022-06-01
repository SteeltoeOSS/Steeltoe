// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Binding;

/// <summary>
/// Defines methods to create/configure the binding targets defined by EnableBinding
/// </summary>
public interface IBindingTargetFactory
{
    /// <summary>
    /// Checks whether a specific binding target type can be created by this factory.
    /// </summary>
    /// <param name="type">the binding target type</param>
    /// <returns>true if binding target can be created</returns>
    bool CanCreate(Type type);

    /// <summary>
    /// Create an input binding target that will be bound via a corresponding Binder
    /// </summary>
    /// <param name="name">the name of the binding target</param>
    /// <returns>the binding target</returns>
    object CreateInput(string name);

    /// <summary>
    /// Create an output binding target that will be bound via a corresponding Binder
    /// </summary>
    /// <param name="name">the name of the binding target</param>
    /// <returns>the binding target</returns>
    object CreateOutput(string name);
}

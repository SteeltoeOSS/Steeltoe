// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

/// <summary>
/// Represents a binding between an input or output and an adapter endpoint that connects via a Binder.The binding could be for a consumer or a producer.
/// A consumer binding represents a connection from an adapter to an input. A producer binding represents a connection from an output to an adapter.
/// </summary>
public interface IBinding : IPausable
{
    /// <summary>
    /// Gets the extended info associated with the binding.
    /// </summary>
    IDictionary<string, object> ExtendedInfo { get; }

    /// <summary>
    /// Gets the name of the destination for this binding.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the target for this binding (i.e., channel name).
    /// </summary>
    string BindingName { get; }

    /// <summary>
    /// Gets a value indicating whether this binding is an input binding.
    /// </summary>
    bool IsInput { get; }

    /// <summary>
    /// Unbinds the target component represented by this instance and stops any active components.
    /// </summary>
    /// <returns>
    /// task to signal results.
    /// </returns>
    Task Unbind();
}

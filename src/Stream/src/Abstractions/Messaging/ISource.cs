// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Messaging;

/// <summary>
/// Bindable interface with one output channel.
/// </summary>
public interface ISource
{
    /// <summary>
    /// Default name of the output channel
    /// </summary>
    const string OUTPUT = "output";

    /// <summary>
    /// Gets the output channel
    /// </summary>
    [Output(OUTPUT)]
    IMessageChannel Output { get; }
}

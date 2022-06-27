// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Messaging;

/// <summary>
/// Bindable interface with one input channel.
/// </summary>
public interface ISink
{
    /// <summary>
    /// Default channel name.
    /// </summary>
    const string INPUT = "input";

    /// <summary>
    /// Gets the input channel.
    /// </summary>
    [Input(INPUT)]
    ISubscribableChannel Input { get; }
}

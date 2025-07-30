// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

/// <summary>
/// Base interface for any component that can create and send messages
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IMessageProducer
{
    /// <summary>
    /// Gets or sets the output channel the producer uses
    /// </summary>
    IMessageChannel OutputChannel { get; set; }

    /// <summary>
    /// Gets or sets the output channel name the producer uses
    /// </summary>
    string OutputChannelName { get; set; }
}
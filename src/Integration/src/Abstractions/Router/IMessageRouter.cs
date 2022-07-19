// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

/// <summary>
///  Routers implementing this interface have a default output channel.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Gets the default output channel.
    /// </summary>
    IMessageChannel DefaultOutputChannel { get; }
}

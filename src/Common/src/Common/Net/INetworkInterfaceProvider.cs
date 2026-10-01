// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Common.Net;

/// <summary>
/// Provides access to the low-level, network-dependent operations used by <see cref="InetUtils" />, so they can be faked in tests.
/// </summary>
internal interface INetworkInterfaceProvider
{
    /// <summary>
    /// Gets a snapshot of all network interfaces currently known to the operating system.
    /// </summary>
    IReadOnlyList<NetworkInterfaceSnapshot> GetAllNetworkInterfaces();

    /// <summary>
    /// Performs a reverse DNS lookup to obtain the hostname associated with the specified IP address.
    /// </summary>
    /// <param name="address">
    /// The IP address to resolve.
    /// </param>
    /// <returns>
    /// The hostname associated with the IP address.
    /// </returns>
    string ResolveHostName(IPAddress address);
}

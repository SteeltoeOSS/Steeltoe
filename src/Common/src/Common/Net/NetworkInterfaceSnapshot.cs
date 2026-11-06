// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Common.Net;

/// <summary>
/// Represents the properties of a single network interface that <see cref="InetUtils" /> considers when selecting a non-loopback address.
/// </summary>
/// <param name="Name">
/// The name of the network interface.
/// </param>
/// <param name="Id">
/// The identifier of the network interface, used for diagnostic logging only.
/// </param>
/// <param name="IsUp">
/// Whether the network interface is operational.
/// </param>
/// <param name="IsReceiveOnly">
/// Whether the network interface can only receive data and cannot be used to transmit data.
/// </param>
/// <param name="IndexIPv4">
/// The IPv4 interface index, used to order interfaces. Only meaningful when <paramref name="IsUp" /> is <c>true</c> and
/// <paramref name="IsReceiveOnly" /> is <c>false</c>.
/// </param>
/// <param name="UnicastAddresses">
/// The unicast IP addresses assigned to this network interface. Only populated when <paramref name="IsUp" /> is <c>true</c> and
/// <paramref name="IsReceiveOnly" /> is <c>false</c>.
/// </param>
internal sealed record NetworkInterfaceSnapshot(
    string Name, string Id, bool IsUp, bool IsReceiveOnly, int IndexIPv4, IReadOnlyList<IPAddress> UnicastAddresses);

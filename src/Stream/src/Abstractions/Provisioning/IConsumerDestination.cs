// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Provisioning;

/// <summary>
/// Represents a ConsumerDestination that provides the information about the destination
/// that is physically provisioned through a provisioning provider
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IConsumerDestination
{
    /// <summary>
    /// Gets the destination name
    /// </summary>
    string Name { get; }
}
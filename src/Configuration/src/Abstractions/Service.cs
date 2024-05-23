// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration;

/// <summary>
/// Represents a service instance bound to an application.
/// </summary>
public sealed class Service : BaseServiceOptions
{
    /// <summary>
    /// Gets the connection information and credentials for the service.
    /// </summary>
    public IDictionary<string, Credential> Credentials { get; } = new Dictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
}

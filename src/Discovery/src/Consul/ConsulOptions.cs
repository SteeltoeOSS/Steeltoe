// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// Configuration options to use in creating a Consul client. See the documentation of the Consul client for more details.
/// </summary>
public class ConsulOptions
{
    public const string ConsulConfigurationPrefix = "consul";

    /// <summary>
    /// Gets or sets the host address of the Consul server, default localhost.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the scheme used for the Consul server, default http.
    /// </summary>
    public string Scheme { get; set; } = "http";

    /// <summary>
    /// Gets or sets the port number used for the Consul server, default 8500.
    /// </summary>
    public int Port { get; set; } = 8500;

    /// <summary>
    /// Gets or sets the data center to use.
    /// </summary>
    public string Datacenter { get; set; }

    /// <summary>
    /// Gets or sets the access token to use.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the wait time to use.
    /// </summary>
    public string WaitTime { get; set; }

    /// <summary>
    /// Gets or sets the user name to use.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets password to use.
    /// </summary>
    public string Password { get; set; }
}

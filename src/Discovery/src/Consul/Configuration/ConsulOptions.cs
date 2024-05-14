// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Configuration;

/// <summary>
/// Configuration options to use in creating a Consul client. See the documentation of the Consul client for more details.
/// </summary>
public sealed class ConsulOptions
{
    internal const string ConfigurationPrefix = "consul";

    /// <summary>
    /// Gets or sets the host name or IP address of the Consul server. Default value: localhost.
    /// </summary>
    public string? Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the scheme to connect with the Consul server ("http" or "https"). Default value: http.
    /// </summary>
    public string? Scheme { get; set; } = "http";

    /// <summary>
    /// Gets or sets the port number the Consul server is listening on. Default value: 8500.
    /// </summary>
    public int Port { get; set; } = 8500;

    /// <summary>
    /// Gets or sets the datacenter name passed in each request to the server.
    /// </summary>
    public string? Datacenter { get; set; }

    /// <summary>
    /// Gets or sets the authentication token passed in each request to the server.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration for a blocking request.
    /// </summary>
    public string? WaitTime { get; set; }

    /// <summary>
    /// Gets or sets the username for HTTP authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets password for HTTP authentication.
    /// </summary>
    public string? Password { get; set; }
}

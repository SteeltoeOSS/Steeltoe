// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaServerOptions
{
    internal const int DefaultConnectTimeoutSeconds = 5;

    /// <summary>
    /// Gets or sets the proxy hostname used in contacting the Eureka server.
    /// </summary>
    public string? ProxyHost { get; set; }

    /// <summary>
    /// Gets or sets the proxy port number used in contacting the Eureka server.
    /// </summary>
    public int ProxyPort { get; set; }

    /// <summary>
    /// Gets or sets the proxy username used in contacting the Eureka server.
    /// </summary>
    public string? ProxyUserName { get; set; }

    /// <summary>
    /// Gets or sets the proxy password used in contacting the Eureka server.
    /// </summary>
    public string? ProxyPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-decompress responses from Eureka server. The registry information from the Eureka server is
    /// compressed for optimum network traffic. Default value: true.
    /// </summary>
    public bool ShouldGZipContent { get; set; } = true;

    /// <summary>
    /// Gets or sets how long to wait (in seconds) before a connection to the Eureka server times out. Note that the connections in the client are pooled and
    /// this setting affects the actual connection creation and also the wait time to get the connection from the pool. Default value: 5.
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = DefaultConnectTimeoutSeconds;

    /// <summary>
    /// Gets or sets the number of times to retry Eureka server requests. Default value: 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;
}

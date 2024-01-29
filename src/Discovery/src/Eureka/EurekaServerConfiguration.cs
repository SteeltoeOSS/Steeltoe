// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaServerConfiguration
{
    internal const int DefaultConnectTimeoutSeconds = 5;

    /// <summary>
    /// Gets or sets the proxy host to the Eureka server, if any. Configuration property: eureka:client:eurekaServer:proxyHost.
    /// </summary>
    public string? ProxyHost { get; set; }

    /// <summary>
    /// Gets or sets the proxy port to the Eureka server, if any. Configuration property: eureka:client:eurekaServer:proxyPort.
    /// </summary>
    public int ProxyPort { get; set; }

    /// <summary>
    /// Gets or sets the proxy username, if any. Configuration property: eureka:client:eurekaServer:proxyUserName.
    /// </summary>
    public string? ProxyUserName { get; set; }

    /// <summary>
    /// Gets or sets the proxy password, if any. Configuration property: eureka:client:eurekaServer:proxyPassword.
    /// </summary>
    public string? ProxyPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the content fetched from the Eureka server has to be compressed whenever it is supported by the server. The
    /// registry information from the Eureka server is compressed for optimum network traffic. Configuration property:
    /// eureka:client:eurekaServer:shouldGZipContent.
    /// </summary>
    public bool ShouldGZipContent { get; set; } = true;

    /// <summary>
    /// Gets or sets how long to wait (in seconds) before a connection to the Eureka server times out. Note that the connections in the client are pooled and
    /// this setting affects the actual connection creation and also the wait time to get the connection from the pool. Configuration property:
    /// eureka:client:eurekaServer:connectTimeoutSeconds.
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = DefaultConnectTimeoutSeconds;

    /// <summary>
    /// Gets or sets the number of times to retry Eureka Server requests. Configuration property: eureka:client:eurekaServer:retryCount.
    /// </summary>
    public int RetryCount { get; set; } = 3;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.NetworkInformation;
using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public sealed class SpringBootAdminClientOptions : IValidateCertificatesOptions
{
    internal TimeSpan ConnectionTimeout => TimeSpan.FromMilliseconds(ConnectionTimeoutMs);
    internal string? BasePathRooted => BasePath == null ? null : '/' + BasePath.TrimStart('/');

    /// <summary>
    /// Gets or sets the URL of the Spring Boot Admin server.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the name used to register this application with in Spring Boot Admin server.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the base URL of this application, which Spring Boot Admin server uses to interact with this application.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the scheme ("http" or "https") to use in <see cref="BaseUrl" />.
    /// </summary>
    public string? BaseScheme { get; set; }

    /// <summary>
    /// Gets or sets the hostname or IP address to use in <see cref="BaseUrl" />. This value should be set explicitly if Spring Boot Admin server is running
    /// in a container.
    /// </summary>
    public string? BaseHost { get; set; }

    /// <summary>
    /// Gets or sets the port number to use in <see cref="BaseUrl" />.
    /// </summary>
    public int? BasePort { get; set; }

    /// <summary>
    /// Gets or sets the path to use in <see cref="BaseUrl" />.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use <see cref="NetworkInterface.GetAllNetworkInterfaces" /> to determine the IP address or hostname for
    /// <see cref="BaseUrl" />. Default value: false.
    /// </summary>
    public bool UseNetworkInterfaces { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefer using the local IP address over hostname in <see cref="BaseUrl" />. Default value: false.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate certificates from Spring Boot Admin server. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout (in milliseconds) for interactions with Spring Boot Admin server. Default value: 5_000.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// Gets or sets the interval for refreshing the registration with Spring Boot Admin server. Set to <see cref="TimeSpan.Zero" /> to register only once at
    /// app startup. Default value: 10 seconds.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets metadata to use when registering with Spring Boot Admin server.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public sealed class SpringBootAdminClientOptions : IValidateCertificatesOptions
{
    internal TimeSpan ConnectionTimeout => TimeSpan.FromMilliseconds(ConnectionTimeoutMs);

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
    public string? BasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate certificates from Spring Boot Admin server. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout (in milliseconds) for interactions with Spring Boot Admin server. Default value: 100_000.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 100_000;

    /// <summary>
    /// Gets metadata to use when registering with Spring Boot Admin server.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
}

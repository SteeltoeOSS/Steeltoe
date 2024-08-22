// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public sealed class SpringBootAdminClientOptions : IValidateCertificatesOptions
{
    internal TimeSpan ConnectionTimeout => TimeSpan.FromMilliseconds(ConnectionTimeoutMs);

    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the name to use for this application when registering with SBA.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the base path SBA should use for interacting with your application.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SBA certificates should be validated. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout (in milliseconds) for interactions with SBA. Default value: 100_000.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 100_000;

    /// <summary>
    /// Gets metadata to use when registering with SBA.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
}

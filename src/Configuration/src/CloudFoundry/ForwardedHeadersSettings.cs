// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry;

public sealed class ForwardedHeadersSettings
{
    internal const string ConfigurationKey = "Steeltoe:ForwardedHeaders";

    /// <summary>
    /// Gets or sets a value indicating whether to trust all networks (adds 0.0.0.0/0). WARNING: Use only behind a trusted ingress. Default value: true.
    /// </summary>
    public bool TrustAllNetworks { get; set; } = true;

    /// <summary>
    /// Gets or sets known networks to trust, in CIDR notation. Example: "10.0.0.0/8,192.168.0.0/16".
    /// </summary>
    public string? KnownNetworks { get; set; }
}

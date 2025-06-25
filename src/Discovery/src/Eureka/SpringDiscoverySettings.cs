// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Fallback configuration settings for Eureka.
/// </summary>
internal sealed class SpringDiscoverySettings
{
    // This type only exists to enable JSON schema documentation via ConfigurationSchemaAttribute.

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Eureka client. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how to register on Cloud Foundry. Can be "route", "direct", or "hostname".
    /// </summary>
    public string? RegistrationMethod { get; set; }
}

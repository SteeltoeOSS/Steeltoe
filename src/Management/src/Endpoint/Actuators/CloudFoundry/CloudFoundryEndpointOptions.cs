// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

public sealed class CloudFoundryEndpointOptions : EndpointOptions, IValidateCertificatesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to validate certificates. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the GUID identifying the app, used in permission checks.
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the location of the Cloud Controller API for the Cloud Foundry deployment where the app runs.
    /// </summary>
    [ConfigurationKeyName("CloudFoundryApi")]
    public string? Api { get; set; }
}

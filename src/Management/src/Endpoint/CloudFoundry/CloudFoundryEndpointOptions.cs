// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryEndpointOptions : EndpointOptionsBase//, ICloudFoundryOptions
{
    private const bool DefaultValidateCertificates = true;

    public bool ValidateCertificates { get; set; } = DefaultValidateCertificates;

    public string ApplicationId { get; set; }

    public string CloudFoundryApi { get; set; }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryEndpointOptions : AbstractEndpointOptions, ICloudFoundryOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:cloudfoundry";
    private const string VcapApplicationIdKey = "vcap:application:application_id";
    private const string VcapApplicationCloudfoundryApiKey = "vcap:application:cf_api";
    private const bool DefaultValidateCertificates = true;

    public bool ValidateCertificates { get; set; } = DefaultValidateCertificates;

    public string ApplicationId { get; set; }

    public string CloudFoundryApi { get; set; }

    public CloudFoundryEndpointOptions()
    {
        Id = string.Empty;
    }

    public CloudFoundryEndpointOptions(IConfiguration config)
        : base(ManagementInfoPrefix, config)
    {
        Id = string.Empty;
        ApplicationId = config[VcapApplicationIdKey];
        CloudFoundryApi = config[VcapApplicationCloudfoundryApiKey];
    }
}

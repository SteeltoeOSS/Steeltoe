// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class ConfigureCloudFoundryEndpointOptions : ConfigureEndpointOptions<CloudFoundryEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:cloudfoundry";
    private const string VcapApplicationIdKey = "vcap:application:application_id";
    private const string VcapApplicationCloudfoundryApiKey = "vcap:application:cf_api";

    public ConfigureCloudFoundryEndpointOptions(IConfiguration configuration): base(configuration, ManagementInfoPrefix, string.Empty)
    {
    }
    public override void Configure(CloudFoundryEndpointOptions options)
    {
        base.Configure(options);
        options.ApplicationId = configuration[VcapApplicationIdKey];
        options.CloudFoundryApi = configuration[VcapApplicationCloudfoundryApiKey];
    }
   
}

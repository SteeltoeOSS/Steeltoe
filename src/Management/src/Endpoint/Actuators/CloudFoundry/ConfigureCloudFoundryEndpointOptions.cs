// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

internal sealed class ConfigureCloudFoundryEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<CloudFoundryEndpointOptions>(configuration, ManagementInfoPrefix, string.Empty)
{
    private const string ManagementInfoPrefix = "management:endpoints:cloudfoundry";
    private const string VcapApplicationIdKey = "vcap:application:application_id";
    private const string VcapApplicationCloudfoundryApiKey = "vcap:application:cf_api";

    private readonly IConfiguration _configuration = configuration;

    public override void Configure(CloudFoundryEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);
        options.ApplicationId ??= _configuration[VcapApplicationIdKey];
        options.Api ??= _configuration[VcapApplicationCloudfoundryApiKey];
    }
}

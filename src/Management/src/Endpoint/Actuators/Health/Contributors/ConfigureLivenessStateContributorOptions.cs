// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal sealed class ConfigureLivenessStateContributorOptions : IConfigureOptionsWithKey<LivenessStateContributorOptions>
{
    private readonly IConfiguration _configuration;

    public string ConfigurationKey => "management:endpoints:health:liveness";

    public ConfigureLivenessStateContributorOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(LivenessStateContributorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ConfigurationKey).Bind(options);
    }
}

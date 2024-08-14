// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

internal sealed class ConfigureDiskSpaceContributorOptions : IConfigureOptionsWithKey<DiskSpaceContributorOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:health:diskspace";
    private readonly IConfiguration _configuration;

    public string ConfigurationKey => ManagementInfoPrefix;

    public ConfigureDiskSpaceContributorOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(DiskSpaceContributorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ManagementInfoPrefix).Bind(options);
    }
}

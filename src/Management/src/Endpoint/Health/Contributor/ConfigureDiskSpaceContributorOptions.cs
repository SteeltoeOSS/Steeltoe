// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

internal sealed class ConfigureDiskSpaceContributorOptions : IConfigureOptions<DiskSpaceContributorOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:health:diskspace";
    private readonly IConfiguration _configuration;

    public ConfigureDiskSpaceContributorOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(DiskSpaceContributorOptions options)
    {
        ArgumentGuard.NotNull(options);

        _configuration.GetSection(ManagementInfoPrefix).Bind(options);
    }
}

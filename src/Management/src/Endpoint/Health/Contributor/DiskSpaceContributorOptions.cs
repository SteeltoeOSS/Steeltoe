// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

public class DiskSpaceContributorOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:health:diskspace";
    private const long DefaultThreshold = 10 * 1024 * 1024;

    public string Path { get; set; }

    public long Threshold { get; set; } = -1;

    public DiskSpaceContributorOptions()
    {
        Path = ".";
        Threshold = DefaultThreshold;
    }

    public DiskSpaceContributorOptions(IConfiguration config)
    {
        ArgumentGuard.NotNull(config);

        IConfigurationSection section = config.GetSection(ManagementInfoPrefix);

        if (section != null)
        {
            section.Bind(this);
        }

        if (string.IsNullOrEmpty(Path))
        {
            Path = ".";
        }

        if (Threshold == -1)
        {
            Threshold = DefaultThreshold;
        }
    }
}

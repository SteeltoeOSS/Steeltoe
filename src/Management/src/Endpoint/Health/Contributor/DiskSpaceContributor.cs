// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

internal sealed class DiskSpaceContributor : IHealthContributor
{
    private readonly IOptionsMonitor<DiskSpaceContributorOptions> _optionsMonitor;

    public string Id => "diskSpace";

    public DiskSpaceContributor(IOptionsMonitor<DiskSpaceContributorOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public HealthCheckResult? Health()
    {
        DiskSpaceContributorOptions options = _optionsMonitor.CurrentValue;

        if (!options.Enabled || options.Path == null)
        {
            return null;
        }

        var result = new HealthCheckResult();
        string fullPath = Path.GetFullPath(options.Path);
        var dirInfo = new DirectoryInfo(fullPath);

        if (dirInfo.Exists)
        {
            string rootName = dirInfo.Root.Name;
            var driveInfo = new DriveInfo(rootName);
            long freeSpace = driveInfo.TotalFreeSpace;
            result.Status = freeSpace >= options.Threshold ? HealthStatus.Up : HealthStatus.Down;

            result.Details.Add("total", driveInfo.TotalSize);
            result.Details.Add("free", freeSpace);
            result.Details.Add("threshold", options.Threshold);
            result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
        }

        return result;
    }
}

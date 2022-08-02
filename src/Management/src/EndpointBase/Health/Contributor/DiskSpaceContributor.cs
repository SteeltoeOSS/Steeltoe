// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

public class DiskSpaceContributor : IHealthContributor
{
    private const string DefaultId = "diskSpace";
    private readonly DiskSpaceContributorOptions _options;

    public string Id { get; } = DefaultId;

    public DiskSpaceContributor(DiskSpaceContributorOptions options = null)
    {
        _options = options ?? new DiskSpaceContributorOptions();
    }

    public HealthCheckResult Health()
    {
        var result = new HealthCheckResult();

        string fullPath = Path.GetFullPath(_options.Path);
        var dirInfo = new DirectoryInfo(fullPath);

        if (dirInfo.Exists)
        {
            string rootName = dirInfo.Root.Name;
            var d = new DriveInfo(rootName);
            long freeSpace = d.TotalFreeSpace;
            result.Status = freeSpace >= _options.Threshold ? HealthStatus.Up : HealthStatus.Down;

            result.Details.Add("total", d.TotalSize);
            result.Details.Add("free", freeSpace);
            result.Details.Add("threshold", _options.Threshold);
            result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
        }

        return result;
    }
}

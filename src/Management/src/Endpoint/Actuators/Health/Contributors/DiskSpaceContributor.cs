// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal sealed class DiskSpaceContributor : IHealthContributor
{
    private readonly IOptionsMonitor<DiskSpaceContributorOptions> _optionsMonitor;

    public string Id => "diskSpace";

    public DiskSpaceContributor(IOptionsMonitor<DiskSpaceContributorOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        HealthCheckResult? result = Health();
        return Task.FromResult(result);
    }

    private HealthCheckResult? Health()
    {
        DiskSpaceContributorOptions options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(options.Path))
        {
            string absolutePath = Path.GetFullPath(options.Path);

            if (Directory.Exists(absolutePath))
            {
                DriveInfo[] systemDrives = DriveInfo.GetDrives();
                DriveInfo? driveInfo = FindVolume(absolutePath, systemDrives);

                if (driveInfo != null)
                {
                    long freeSpaceInBytes = driveInfo.TotalFreeSpace;

                    var result = new HealthCheckResult
                    {
                        Status = freeSpaceInBytes >= options.Threshold ? HealthStatus.Up : HealthStatus.Down
                    };

                    result.Details.Add("total", driveInfo.TotalSize);
                    result.Details.Add("free", freeSpaceInBytes);
                    result.Details.Add("threshold", options.Threshold);
                    result.Details.Add("path", absolutePath);
                    result.Details.Add("exists", driveInfo.RootDirectory.Exists);
                    return result;
                }
            }
        }

        return new HealthCheckResult
        {
            Status = HealthStatus.Unknown,
            Description = "Failed to determine free disk space.",
            Details =
            {
                ["error"] = "The configured path is invalid or does not exist."
            }
        };
    }

    internal static DriveInfo? FindVolume(string absolutePath, IEnumerable<DriveInfo> systemDrives)
    {
        // Prefer to match "/mnt/data/path/to/directory" against "/mnt/data" over "/".
        DriveInfo? longestMatch = null;

        foreach (DriveInfo drive in systemDrives)
        {
            string volumePath = drive.RootDirectory.FullName;

            if (!PathIsOrStartsWith(absolutePath, volumePath))
            {
                continue;
            }

            if (longestMatch == null || longestMatch.RootDirectory.FullName.Length < drive.RootDirectory.FullName.Length)
            {
                longestMatch = drive;
            }
        }

        return longestMatch;
    }

    private static bool PathIsOrStartsWith(string absolutePath, string volumePath)
    {
        if (!absolutePath.StartsWith(volumePath, StringComparison.OrdinalIgnoreCase))
        {
            // Exit fast if no match is possible.
            return false;
        }

        // "/tmp/someLonger" is not a subdirectory of "/tmp/some".
        // And on Linux, "/tmp/SOME" is not the same as "/tmp/some".

        string relativePath = Path.GetRelativePath(volumePath, absolutePath);
        return !relativePath.StartsWith("..", StringComparison.Ordinal);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

public class DiskSpaceContributorOptions
{
    private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health:diskspace";
    private const long DEFAULT_THRESHOLD = 10 * 1024 * 1024;

    public DiskSpaceContributorOptions()
    {
        Path = ".";
        Threshold = DEFAULT_THRESHOLD;
    }

    public DiskSpaceContributorOptions(IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var section = config.GetSection(MANAGEMENT_INFO_PREFIX);
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
            Threshold = DEFAULT_THRESHOLD;
        }
    }

    public string Path { get; set; }

    public long Threshold { get; set; } = -1;
}
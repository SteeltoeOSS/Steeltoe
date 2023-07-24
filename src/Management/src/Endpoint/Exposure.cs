// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint;

public sealed class Exposure
{
    private const string ExposurePrefix = "management:endpoints:actuator:exposure";
    private const string ExposureSecondChancePrefix = "management:endpoints:web:exposure";

    private static readonly List<string> DefaultIncludes = new()
    {
        "health",
        "info"
    };

    public IList<string> Include { get; set; }
    public IList<string> Exclude { get; set; }

    public Exposure()
        : this(false)
    {
    }

    public Exposure(bool allowAll)
    {
        Include = allowAll
            ? new List<string>
            {
                "*"
            }
            : DefaultIncludes;
    }

    public Exposure(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(ExposurePrefix);

        if (section != null)
        {
            section.Bind(this);
        }

        IConfigurationSection secondSection = configuration.GetSection(ExposureSecondChancePrefix);

        if (secondSection.Exists())
        {
            Include = GetListFromConfigCsvString(secondSection, "include");
            Exclude = GetListFromConfigCsvString(secondSection, "exclude");
        }

        if (Include == null && Exclude == null)
        {
            Include = DefaultIncludes;
        }
    }

    private List<string> GetListFromConfigCsvString(IConfigurationSection configSection, string key)
    {
        return configSection.GetValue<string>(key)?.Split(',').ToList();
    }
}

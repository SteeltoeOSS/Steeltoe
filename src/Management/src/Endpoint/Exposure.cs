// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint;

public sealed class Exposure
{
    private const string Prefix = "management:endpoints:actuator:exposure";
    private const string SecondChancePrefix = "management:endpoints:web:exposure";

    private static readonly List<string> DefaultIncludes = new()
    {
        "health",
        "info"
    };

    public IList<string> Include { get; set; } = new List<string>();
    public IList<string> Exclude { get; set; } = new List<string>();

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

        configuration.GetSection(Prefix).Bind(this);

        IConfigurationSection secondSection = configuration.GetSection(SecondChancePrefix);

        if (secondSection.Exists())
        {
            Include = GetListFromConfigurationCsvString(secondSection, "include") ?? new List<string>();
            Exclude = GetListFromConfigurationCsvString(secondSection, "exclude") ?? new List<string>();
        }

        if (Include.Count == 0 && Exclude.Count == 0)
        {
            Include = DefaultIncludes;
        }
    }

    private List<string>? GetListFromConfigurationCsvString(IConfigurationSection section, string key)
    {
        return section.GetValue<string?>(key)?.Split(',').ToList();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint;

public class Exposure
{
    private const string ExposurePrefix = "management:endpoints:actuator:exposure";
    private const string ExposureSecondChancePrefix = "management:endpoints:web:exposure";

    private static readonly IList<string> DefaultInclude = new List<string>
    {
        "health",
        "info"
    };

    public IList<string> Include { get; set; }

    public IList<string> Exclude { get; set; }

    public Exposure()
    {
        Include = DefaultInclude;
    }

    public Exposure(IConfiguration config)
    {
        IConfigurationSection section = config.GetSection(ExposurePrefix);

        if (section != null)
        {
            section.Bind(this);
        }

        IConfigurationSection secondSection = config.GetSection(ExposureSecondChancePrefix);

        if (secondSection.Exists())
        {
            Include = GetListFromConfigCsvString(secondSection, "include");
            Exclude = GetListFromConfigCsvString(secondSection, "exclude");
        }

        if (Include == null && Exclude == null)
        {
            Include = DefaultInclude;
        }
    }

    private List<string> GetListFromConfigCsvString(IConfigurationSection configSection, string key)
    {
        return configSection.GetValue<string>(key)?.Split(',').ToList();
    }
}

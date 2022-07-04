// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint;

public class Exposure
{
    private const string ExposurePrefix = "management:endpoints:actuator:exposure";
    private const string ExposureSecondchancePrefix = "management:endpoints:web:exposure";
    private static readonly List<string> DefaultInclude = new () { "health", "info" };

    public Exposure()
    {
        Include = DefaultInclude;
    }

    public Exposure(IConfiguration config)
    {
        var section = config.GetSection(ExposurePrefix);
        if (section != null)
        {
            section.Bind(this);
        }

        var secondSection = config.GetSection(ExposureSecondchancePrefix);
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

    public List<string> Include { get; set; }

    public List<string> Exclude { get; set; }

    private List<string> GetListFromConfigCsvString(IConfigurationSection configSection, string key)
        => configSection.GetValue<string>(key)?.Split(',').ToList();
}

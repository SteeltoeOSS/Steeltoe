// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint
{
    public class Exposure
    {
        private const string EXPOSURE_PREFIX = "management:endpoints:actuator:exposure";
        private const string EXPOSURE_SECONDCHANCE_PREFIX = "management:endpoints:web:exposure";
        private static readonly List<string> DEFAULT_INCLUDE = new () { "health", "info" };

        public Exposure()
        {
            Include = DEFAULT_INCLUDE;
        }

        public Exposure(IConfiguration config)
        {
            var section = config.GetSection(EXPOSURE_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            var secondSection = config.GetSection(EXPOSURE_SECONDCHANCE_PREFIX);
            if (secondSection.Exists())
            {
                Include = GetListFromConfigCSVString(secondSection, "include");
                Exclude = GetListFromConfigCSVString(secondSection, "exclude");
            }

            if (Include == null && Exclude == null)
            {
                Include = DEFAULT_INCLUDE;
            }
        }

        public List<string> Include { get; set; }

        public List<string> Exclude { get; set; }

        private List<string> GetListFromConfigCSVString(IConfigurationSection configSection, string key)
            => configSection.GetValue<string>(key)?.Split(',').ToList();
    }
}

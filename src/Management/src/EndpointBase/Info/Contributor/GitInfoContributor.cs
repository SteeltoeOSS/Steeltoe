// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public class GitInfoContributor : AbstractConfigurationContributor, IInfoContributor
    {
        private const string GITSETTINGS_PREFIX = "git";
        private const string GITPROPERTIES_FILE = "git.properties";
        private const string DATETIME_OUTPUT_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

        private readonly List<string> DATETIME_INPUT_KEYS = new List<string> { "time" };
        
        private readonly string _propFile;
        private readonly ILogger _logger;

        public GitInfoContributor(ILogger<GitInfoContributor> logger = null)
            : this(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + GITPROPERTIES_FILE)
        {
            _logger = logger;
        }

        public GitInfoContributor(string propFile, ILogger<GitInfoContributor> logger = null)
            : base()
        {
            _propFile = propFile;
            _logger = logger;
        }

        public virtual void Contribute(IInfoBuilder builder)
        {
            _config = ReadGitProperties(_propFile);
            Contribute(builder, GITSETTINGS_PREFIX, true);
        }

        public virtual IConfiguration ReadGitProperties(string propFile)
        {
            if (File.Exists(propFile))
            {
                var lines = File.ReadAllLines(propFile);
                if (lines != null && lines.Length > 0)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("#") ||
                            !line.StartsWith("git.", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var keyVal = line.Split('=');
                        if (keyVal == null || keyVal.Length != 2)
                        {
                            continue;
                        }

                        var key = keyVal[0].Trim().Replace('.', ':');
                        var val = keyVal[1].Replace("\\:", ":");

                        dict[key] = val;
                    }

                    var builder = new ConfigurationBuilder();
                    builder.AddInMemoryCollection(dict);
                    return builder.Build();
                }
            }
            else
            {
                _logger?.LogWarning("Unable to locate GitInfo at {GitInfoLocation}", propFile);
            }

            return null;
        }

        protected override void AddKeyValue(Dictionary<string, object> dict, string key, string value)
        {
            var valueToInsert = value;

            if (DATETIME_INPUT_KEYS.Contains(key))
            {
                // Normalize datetime values to ISO8601 format
                valueToInsert = DateTime.Parse(value, CultureInfo.InvariantCulture).ToString(DATETIME_OUTPUT_FORMAT);
            }

            dict[key] = valueToInsert;
        }
    }
}

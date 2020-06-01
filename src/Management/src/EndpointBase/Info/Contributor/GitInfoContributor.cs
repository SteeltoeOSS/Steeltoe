// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public class GitInfoContributor : AbstractConfigurationContributor, IInfoContributor
    {
        private const string GITSETTINGS_PREFIX = "git";
        private const string GITPROPERTIES_FILE = "git.properties";

        private static readonly DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
                string[] lines = File.ReadAllLines(propFile);
                if (lines != null && lines.Length > 0)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("#") ||
                            !line.StartsWith("git.", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string[] keyVal = line.Split('=');
                        if (keyVal == null || keyVal.Length != 2)
                        {
                            continue;
                        }

                        string key = keyVal[0].Trim().Replace('.', ':');
                        string val = keyVal[1].Replace("\\:", ":");

                        dict[key] = val;
                    }

                    ConfigurationBuilder builder = new ConfigurationBuilder();
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
            string keyToInsert = key;
            object valueToInsert = value;

            if ("time".Equals(key))
            {
                DateTime dt = DateTime.Parse(value);
                DateTime utc = dt.ToUniversalTime();
                valueToInsert = (utc.Ticks - BaseTime.Ticks) / 10000;
            }

            dict[keyToInsert] = valueToInsert;
        }
    }
}

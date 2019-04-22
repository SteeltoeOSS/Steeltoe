//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public class GitInfoContributor : AbstractConfigurationContributor, IInfoContributor
    {
        private const string GITSETTINGS_PREFIX = "git";
        private const string GITPROPERTIES_FILE = "git.properties";

        private string _propFile;
        public GitInfoContributor() 
            : this(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + GITPROPERTIES_FILE)
        {
        }

        public GitInfoContributor(string propFile) : base()
        {
            _propFile = propFile;
        }

        public virtual void Contribute(IInfoBuilder builder)
        {
            _config = ReadGitProperties(_propFile);
            base.Contribute(builder, GITSETTINGS_PREFIX, true);
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
                valueToInsert = (utc.Ticks - baseTime.Ticks)/10000;
            }

            dict[keyToInsert] = valueToInsert;
        }

        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

}

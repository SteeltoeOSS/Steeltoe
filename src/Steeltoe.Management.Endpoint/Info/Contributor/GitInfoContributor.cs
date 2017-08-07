using System;
using System.Collections.Generic;
using System.Text;
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

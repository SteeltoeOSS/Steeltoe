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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.CloudFoundry.Connector.MongoDb
{
    public class MongoDbConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Server = "localhost";
        public const int Default_Port = 27017;
        private const string MONGODB_CLIENT_SECTION_PREFIX = "mongodb:client";
        private readonly bool cloudFoundryConfigFound = false;

        public MongoDbConnectorOptions()
        {
        }

        public MongoDbConnectorOptions(IConfiguration config)
            : base('&', '=')
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MONGODB_CLIENT_SECTION_PREFIX);
            section.Bind(this);

            Options = config
                .GetSection(MONGODB_CLIENT_SECTION_PREFIX + ":options")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);

            cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
        }

        public string ConnectionString { get; set; }

        public string Server { get; set; } = Default_Server;

        public int Port { get; set; } = Default_Port;

        public string Username { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        internal string Uri { get; set; }

        internal Dictionary<string, string> Options { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !cloudFoundryConfigFound)
            {
                // Connection string was provided and VCAP_SERVICES wasn't found, just use the connectionstring
                return ConnectionString;
            }
            else if (Uri != null)
            {
                // VCAP_SERVICES provided a URI, the MongoDB driver can just use that
                return Uri;
            }
            else
            {
                // build a MongoDB connection string
                StringBuilder sb = new StringBuilder();

                sb.Append("mongodb://");
                AddColonDelimitedPair(sb, Username, Password, '@');
                AddColonDelimitedPair(sb, Server, Port.ToString());

                if (!string.IsNullOrEmpty(Database))
                {
                    sb.Append("/");
                    sb.Append(Database);
                }

                if (Options != null && Options.Any())
                {
                    sb.Append("?");
                    foreach (var o in Options)
                    {
                        AddKeyValue(sb, o.Key, o.Value);
                    }
                }

                return sb.ToString().TrimEnd('&');
            }
        }
    }
}

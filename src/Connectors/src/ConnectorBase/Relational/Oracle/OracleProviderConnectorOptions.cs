// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Oracle
{
    public class OracleProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Server = "localhost";
        public const int Default_Port = 1521;
        private const string ORACLE_CLIENT_SECTION_PREFIX = "oracle:client";
        private bool cloudFoundryConfigFound = false;

        public OracleProviderConnectorOptions()
        {
        }

        public OracleProviderConnectorOptions(IConfiguration config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(ORACLE_CLIENT_SECTION_PREFIX);

            section.Bind(this);

            if (Uri != null)
            {
                Server = Uri.Split(':')[2].Substring(2);
            }

            Username = Uid;
            ServiceName = Service;
            Password = Pw;

            section.Bind(this);

            cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
        }

        public string ConnectionString { get; set; }

        public string Server { get; set; } = Default_Server;

        public int Port { get; set; } = Default_Port;

        public string Username { get; set; }

        public string Password { get; set; }

        public string ServiceName { get; set; }

        public string Uid { get; set; }

        public string Uri { get; set; }

        public string Service { get; set; }

        public string Pw { get; set; }

        internal Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !cloudFoundryConfigFound)
            {
                return ConnectionString;
            }

            return string.Format("User Id={0};Password={1};Data Source={2}:{3}/{4};", Username, Password, Server, Port, ServiceName);
        }
    }
}

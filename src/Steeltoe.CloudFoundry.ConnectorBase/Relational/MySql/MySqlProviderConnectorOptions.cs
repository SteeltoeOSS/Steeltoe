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
using System.Text;

namespace Steeltoe.CloudFoundry.Connector.MySql
{
    /// <summary>
    /// Currently enabling properties supported by BOTH of these connectors:
    /// https://dev.mysql.com/doc/connector-net/en/connector-net-connection-options.html
    /// https://mysql-net.github.io/MySqlConnector/tutorials/migrating-from-connector-net/
    /// </summary>
    public class MySqlProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Server = "localhost";
        public const int Default_Port = 3306;
        private const string MYSQL_CLIENT_SECTION_PREFIX = "mysql:client";
        private bool cloudFoundryConfigFound = false;

        public MySqlProviderConnectorOptions()
        {
        }

        public MySqlProviderConnectorOptions(IConfiguration config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MYSQL_CLIENT_SECTION_PREFIX);
            section.Bind(this);

            cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
        }

        public string ConnectionString { get; set; }

        public string Server { get; set; } = Default_Server;

        public int Port { get; set; } = Default_Port;

        public string Username { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public string SslMode { get; set; }

        public bool? AllowPublicKeyRetrieval { get; set; }

        public bool? AllowUserVariables { get; set; }

        public int? ConnectionTimeout { get; set; }

        public bool? ConvertZeroDateTime { get; set; }

        public int? DefaultCommandTimeout { get; set; }

        public int? Keepalive { get; set; }

        public bool? OldGuids { get; set; }

        public bool? PersistSecurityInfo { get; set; }

        public bool? TreatTinyAsBoolean { get; set; }

        public bool? UseAffectedRows { get; set; }

        public bool? UseCompression { get; set; }

        public int? ConnectionLifeTime { get; set; }

        public bool? ConnectionReset { get; set; }

        public int? MaximumPoolsize { get; set; }

        public int? MinimumPoolsize { get; set; }

        public bool? Pooling { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !cloudFoundryConfigFound)
            {
                return ConnectionString;
            }

            StringBuilder sb = new StringBuilder();
            AddKeyValue(sb, nameof(Server), Server);
            AddKeyValue(sb, nameof(Port), Port);
            AddKeyValue(sb, nameof(Username), Username);
            AddKeyValue(sb, nameof(Password), Password);
            AddKeyValue(sb, nameof(Database), Database);
            AddKeyValue(sb, nameof(SslMode), SslMode);
            AddKeyValue(sb, nameof(AllowPublicKeyRetrieval), AllowPublicKeyRetrieval);
            AddKeyValue(sb, nameof(AllowUserVariables), AllowUserVariables);
            AddKeyValue(sb, nameof(ConnectionLifeTime), ConnectionLifeTime);
            AddKeyValue(sb, nameof(ConnectionReset), ConnectionReset);
            AddKeyValue(sb, nameof(ConnectionTimeout), ConnectionTimeout);
            AddKeyValue(sb, nameof(ConvertZeroDateTime), ConvertZeroDateTime);
            AddKeyValue(sb, nameof(DefaultCommandTimeout), DefaultCommandTimeout);
            AddKeyValue(sb, nameof(Keepalive), Keepalive);
            AddKeyValue(sb, nameof(MaximumPoolsize), MaximumPoolsize);
            AddKeyValue(sb, nameof(MinimumPoolsize), MinimumPoolsize);
            AddKeyValue(sb, nameof(OldGuids), OldGuids);
            AddKeyValue(sb, nameof(PersistSecurityInfo), PersistSecurityInfo);
            AddKeyValue(sb, nameof(Pooling), Pooling);
            AddKeyValue(sb, nameof(TreatTinyAsBoolean), TreatTinyAsBoolean);
            AddKeyValue(sb, nameof(UseAffectedRows), UseAffectedRows);
            AddKeyValue(sb, nameof(UseCompression), UseCompression);
            return sb.ToString();
        }
    }
}

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
    /// https://dev.mysql.com/doc/connector-net/en/connector-net-connection-options.html
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

        public bool? AllowBatch { get; set; }

        public bool? AllowUserVariables { get; set; }

        public bool? AutoEnlist { get; set; }

        public bool? CheckParameters { get; set; }

        public int? ConnectionTimeout { get; set; }

        public int? DefaultCommandTimeout { get; set; }

        public int? DefaultTableCacheAge { get; set; }

        public bool? EnableSessionExpireCallback { get; set; }

        public bool? FunctionsReturnString { get; set; }

        public bool? IgnorePrepare { get; set; }

        public bool? IncludeSecurityAsserts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether various pieces of information are output to any configured TraceListeners
        /// </summary>
        /// <remarks>Currently not supported for .NET Core implementations.</remarks>
        public bool? Logging { get; set; }

        public bool? OldGuids { get; set; }

        public bool? OldSyntax { get; set; }

        public bool? PersistSecurityInfo { get; set; }

        public bool? SqlServerMode { get; set; }

        public bool? TableCache { get; set; }

        public bool? TreatBlobsAsUTF8 { get; set; }

        public bool? TreatTinyAsBoolean { get; set; }

        public bool? UseAffectedRows { get; set; }

        public bool? UseProcedureBodies { get; set; }

        public bool? UseCompression { get; set; }

        public bool? UseUsageAdvisor { get; set; }

        public bool? UsePerformanceMonitor { get; set; }

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
            AddKeyValue(sb, nameof(AllowBatch), AllowBatch);
            AddKeyValue(sb, nameof(AllowUserVariables), AllowUserVariables);
            AddKeyValue(sb, nameof(AutoEnlist), AutoEnlist);
            AddKeyValue(sb, nameof(CheckParameters), CheckParameters);
            AddKeyValue(sb, nameof(ConnectionTimeout), ConnectionTimeout);
            AddKeyValue(sb, nameof(DefaultCommandTimeout), DefaultCommandTimeout);
            AddKeyValue(sb, nameof(DefaultTableCacheAge), DefaultTableCacheAge);
            AddKeyValue(sb, nameof(EnableSessionExpireCallback), EnableSessionExpireCallback);
            AddKeyValue(sb, nameof(FunctionsReturnString), FunctionsReturnString);
            AddKeyValue(sb, nameof(IgnorePrepare), IgnorePrepare);
            AddKeyValue(sb, nameof(IncludeSecurityAsserts), IncludeSecurityAsserts);
            AddKeyValue(sb, nameof(Logging), Logging);
            AddKeyValue(sb, nameof(OldGuids), OldGuids);
            AddKeyValue(sb, nameof(OldSyntax), OldSyntax);
            AddKeyValue(sb, nameof(PersistSecurityInfo), PersistSecurityInfo);
            AddKeyValue(sb, nameof(SqlServerMode), SqlServerMode);
            AddKeyValue(sb, nameof(TableCache), TableCache);
            AddKeyValue(sb, nameof(TreatBlobsAsUTF8), TreatBlobsAsUTF8);
            AddKeyValue(sb, nameof(TreatTinyAsBoolean), TreatTinyAsBoolean);
            AddKeyValue(sb, nameof(UseAffectedRows), UseAffectedRows);
            AddKeyValue(sb, nameof(UseProcedureBodies), UseProcedureBodies);
            AddKeyValue(sb, nameof(UseCompression), UseCompression);
            AddKeyValue(sb, nameof(UseUsageAdvisor), UseUsageAdvisor);
            AddKeyValue(sb, nameof(UsePerformanceMonitor), UsePerformanceMonitor);
            return sb.ToString();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public class SqlServerProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Server = "localhost";
        public const int Default_Port = 1433;
        private const string SQL_CLIENT_SECTION_PREFIX = "sqlserver:credentials";
        private bool _cloudFoundryConfigFound = false;

        public SqlServerProviderConnectorOptions()
        {
        }

        public SqlServerProviderConnectorOptions(IConfiguration config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(SQL_CLIENT_SECTION_PREFIX);

            section.Bind(this);

            if (Uri != null)
            {
                Server = Uri.Split(':')[2].Substring(2);
            }

            Username = Uid;
            Database = Db;
            Password = Pw;

            section.Bind(this);

            _cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
        }

        public string ConnectionString { get; set; }

        public string Server { get; set; } = Default_Server;

        public string InstanceName { get; set; }

        /// <summary>
        /// Gets or sets the port SQL Server is listening on. To exclude from connection string, use a value less than 0
        /// </summary>
        public int Port { get; set; } = Default_Port;

        public string Username { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public string IntegratedSecurity { get; set; }

        public string Uid { get; set; }

        public string Uri { get; set; }

        public string Db { get; set; }

        public string Pw { get; set; }

        /// <summary>
        /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
        /// </summary>
        /// <remarks>Default value is 15</remarks>
        public int? Timeout { get; set; }

        internal Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
            {
                return ConnectionString;
            }

            var sb = new StringBuilder();
            AddDataSource(sb);
            AddKeyValue(sb, "Initial Catalog", Database);
            AddKeyValue(sb, "User Id", Username);
            AddKeyValue(sb, "Password", Password);
            AddKeyValue(sb, "Integrated Security", IntegratedSecurity);
            AddKeyValue(sb, nameof(Timeout), Timeout);

            if (Options != null && Options.Any())
            {
                foreach (var o in Options)
                {
                    AddKeyValue(sb, o.Key, o.Value);
                }
            }

            return sb.ToString();
        }

        private void AddDataSource(StringBuilder sb)
        {
            sb.Append("Data Source");
            sb.Append(Default_Separator);
            sb.Append(Server);

            if (InstanceName != null)
            {
                sb.Append($"\\{InstanceName}");
            }

            if (Port > 0)
            {
                sb.Append($",{Port}");
            }

            sb.Append(Default_Terminator);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private readonly bool _cloudFoundryConfigFound = false;

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

            _cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
        }

        public string ConnectionString { get; set; }

        public string Server { get; set; } = Default_Server;

        public int Port { get; set; } = Default_Port;

        public string Username { get; set; }

        public string Password { get; set; }

        public string ServiceName { get; set; }

        internal Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
            {
                return ConnectionString;
            }

            return string.Format("User Id={0};Password={1};Data Source={2}:{3}/{4};", Username, Password, Server, Port, ServiceName);
        }
    }
}

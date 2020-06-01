// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public class SqlServerProviderConnectorFactory
    {
        private SqlServerServiceInfo _info;
        private SqlServerProviderConnectorOptions _config;
        private SqlServerProviderConfigurer _configurer = new SqlServerProviderConfigurer();

        public SqlServerProviderConnectorFactory()
        {
        }

        public SqlServerProviderConnectorFactory(SqlServerServiceInfo sinfo, SqlServerProviderConnectorOptions config, Type type)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _info = sinfo;
            ConnectorType = type;
        }

        protected Type ConnectorType { get; set; }

        public virtual object Create(IServiceProvider provider)
        {
            var connectionString = CreateConnectionString();
            object result = null;
            if (connectionString != null)
            {
                result = CreateConnection(connectionString);
            }

            if (result == null)
            {
                throw new ConnectorException(string.Format("Unable to create instance of '{0}'", ConnectorType));
            }

            return result;
        }

        public virtual string CreateConnectionString()
        {
            return _configurer.Configure(_info, _config);
        }

        public virtual object CreateConnection(string connectionString)
        {
            return ConnectorHelpers.CreateInstance(ConnectorType, new object[] { connectionString });
        }
    }
}

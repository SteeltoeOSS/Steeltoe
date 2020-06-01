// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.PostgreSql
{
    public class PostgresProviderConnectorFactory
    {
        private PostgresServiceInfo _info;
        private PostgresProviderConnectorOptions _config;
        private PostgresProviderConfigurer _configurer = new PostgresProviderConfigurer();
        private Type _type;

        public PostgresProviderConnectorFactory(PostgresServiceInfo sinfo, PostgresProviderConnectorOptions config, Type type)
        {
            _info = sinfo;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _type = type;
        }

        internal PostgresProviderConnectorFactory()
        {
        }

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
                throw new ConnectorException(string.Format("Unable to create instance of '{0}'", _type));
            }

            return result;
        }

        public virtual string CreateConnectionString()
        {
            return _configurer.Configure(_info, _config);
        }

        public virtual object CreateConnection(string connectionString)
        {
            return ConnectorHelpers.CreateInstance(_type, new object[] { connectionString });
        }
    }
}

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

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
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
            return ReflectionHelpers.CreateInstance(ConnectorType, new object[] { connectionString });
        }
    }
}

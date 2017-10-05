//
// Copyright 2015 the original author or authors.
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
//

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public class SqlServerProviderConnectorFactory
    {
        protected SqlServerServiceInfo _info;
        protected SqlServerProviderConnectorOptions _config;
        protected SqlServerProviderConfigurer _configurer = new SqlServerProviderConfigurer();

        protected Type _type;

        internal SqlServerProviderConnectorFactory()
        {
        }

        public SqlServerProviderConnectorFactory(SqlServerServiceInfo sinfo, SqlServerProviderConnectorOptions config, Type type)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _info = sinfo;
            _config = config;
            _type = type;
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

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

using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.CloudFoundry.Connector.CosmosDb
{
    public class CosmosDbConnectorFactory
    {
        private readonly CosmosDbServiceInfo _info;
        private readonly CosmosDbConnectorOptions _config;
        private readonly CosmosDbProviderConfigurer _configurer = new CosmosDbProviderConfigurer();

        public CosmosDbConnectorFactory()
        {
        }

        public CosmosDbConnectorFactory(CosmosDbServiceInfo sinfo, CosmosDbConnectorOptions config, Type type)
        {
            _info = sinfo;
            _config = config ?? throw new ArgumentNullException(nameof(config));
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

        public virtual object CreateConnection(string connectionString, object clientOptions = null)
        {
            clientOptions ??= Activator.CreateInstance(CosmosDbTypeLocator.CosmosClientOptions);
            return ReflectionHelpers.CreateInstance(ConnectorType, new object[] { connectionString, clientOptions });
        }
    }
}

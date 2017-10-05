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

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Hystrix
{
    public class HystrixProviderConnectorFactory
    {
        protected HystrixRabbitServiceInfo _info;
        protected HystrixProviderConnectorOptions _config;
        protected HystrixProviderConfigurer _configurer = new HystrixProviderConfigurer();
        protected Type _type;

        protected MethodInfo _setUri;

        internal HystrixProviderConnectorFactory()
        {
        }

        public HystrixProviderConnectorFactory(HystrixRabbitServiceInfo sinfo, HystrixProviderConnectorOptions config, Type connectFactory)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

           if (connectFactory == null)
            {
                throw new ArgumentNullException(nameof(connectFactory));
            }

            _info = sinfo;
            _config = config;
            _type = connectFactory;
            _setUri = FindSetUriMethod(_type);
            if (_setUri == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory.SetUri(), incompatible RabbitMQ assembly");
            }
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
            object inst = ConnectorHelpers.CreateInstance(_type, null);
            if (inst == null)
            {
                return null;
            }

            Uri uri = new Uri(connectionString, UriKind.Absolute);

            ConnectorHelpers.Invoke(_setUri, inst, new object[] { uri });
            return new HystrixConnectionFactory(inst);
        }

        public static MethodInfo FindSetUriMethod(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (MethodInfo ci in declaredMethods)
            {
                if (ci.Name.Equals("SetUri"))
                {
                    return ci;
                }
            }

            return null;
        }
    }
}

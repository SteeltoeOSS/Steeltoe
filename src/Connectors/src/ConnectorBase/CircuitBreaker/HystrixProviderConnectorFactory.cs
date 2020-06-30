// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.Hystrix
{
    public class HystrixProviderConnectorFactory
    {
        private HystrixRabbitMQServiceInfo _info;
        private HystrixProviderConnectorOptions _config;
        private HystrixProviderConfigurer _configurer = new HystrixProviderConfigurer();
        private Type _type;

        private MethodInfo _setUri;

        public HystrixProviderConnectorFactory(HystrixRabbitMQServiceInfo sinfo, HystrixProviderConnectorOptions config, Type connectFactory)
        {
            _info = sinfo;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _type = connectFactory ?? throw new ArgumentNullException(nameof(connectFactory));
            _setUri = FindSetUriMethod(_type);
            if (_setUri == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory.SetUri(), incompatible RabbitMQ assembly");
            }
        }

        internal HystrixProviderConnectorFactory()
        {
        }

        public static MethodInfo FindSetUriMethod(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (var ci in declaredMethods)
            {
                if (ci.Name.Equals("SetUri"))
                {
                    return ci;
                }
            }

            return null;
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
            var inst = ReflectionHelpers.CreateInstance(_type, null);
            if (inst == null)
            {
                return null;
            }

            var uri = new Uri(connectionString, UriKind.Absolute);

            ReflectionHelpers.Invoke(_setUri, inst, new object[] { uri });
            return new HystrixConnectionFactory(inst);
        }
    }
}

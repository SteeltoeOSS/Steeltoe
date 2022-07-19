// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.Hystrix;

public class HystrixProviderConnectorFactory
{
    private readonly HystrixRabbitMQServiceInfo _info;
    private readonly HystrixProviderConnectorOptions _config;
    private readonly HystrixProviderConfigurer _configurer = new ();
    private readonly Type _type;

    private readonly MethodInfo _setUri;

    public HystrixProviderConnectorFactory(HystrixRabbitMQServiceInfo serviceInfo, HystrixProviderConnectorOptions options, Type connectFactory)
    {
        _info = serviceInfo;
        _config = options ?? throw new ArgumentNullException(nameof(options));
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
            throw new ConnectorException($"Unable to create instance of '{_type}'");
        }

        return result;
    }

    public virtual string CreateConnectionString()
    {
        return _configurer.Configure(_info, _config);
    }

    public virtual object CreateConnection(string connectionString)
    {
        var inst = ReflectionHelpers.CreateInstance(_type);
        if (inst == null)
        {
            return null;
        }

        var uri = new Uri(connectionString, UriKind.Absolute);

        ReflectionHelpers.Invoke(_setUri, inst, new object[] { uri });
        return new HystrixConnectionFactory(inst);
    }
}

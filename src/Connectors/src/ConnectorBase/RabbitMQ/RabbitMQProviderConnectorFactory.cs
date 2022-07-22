// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.RabbitMQ;

public class RabbitMQProviderConnectorFactory
{
    private readonly RabbitMQServiceInfo _info;
    private readonly RabbitMQProviderConnectorOptions _config;
    private readonly RabbitMQProviderConfigurer _configurer = new ();
    private readonly MethodInfo _setUri;

    public RabbitMQProviderConnectorFactory(RabbitMQServiceInfo sinfo, RabbitMQProviderConnectorOptions config, Type connectFactory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        ConnectorType = connectFactory ?? throw new ArgumentNullException(nameof(connectFactory));
        _info = sinfo;
        _setUri = FindSetUriMethod(ConnectorType);
        if (_setUri == null)
        {
            throw new ConnectorException("Unable to find ConnectionFactory.SetUri(), incompatible RabbitMQ assembly");
        }
    }

    internal RabbitMQProviderConnectorFactory()
    {
    }

    protected Type ConnectorType { get; set; }

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
        var inst = ReflectionHelpers.CreateInstance(ConnectorType, null);
        if (inst == null)
        {
            return null;
        }

        var uri = new Uri(connectionString, UriKind.Absolute);

        ReflectionHelpers.Invoke(_setUri, inst, new object[] { uri });
        return inst;
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.RabbitMQ;

public class RabbitMQProviderConnectorFactory
{
    private readonly RabbitMQServiceInfo _info;
    private readonly RabbitMQProviderConnectorOptions _config;
    private readonly RabbitMQProviderConfigurer _configurer = new();
    private readonly MethodInfo _setUri;

    protected Type ConnectorType { get; set; }

    public RabbitMQProviderConnectorFactory(RabbitMQServiceInfo serviceInfo, RabbitMQProviderConnectorOptions options, Type connectFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(connectFactory);

        _config = options;
        ConnectorType = connectFactory;
        _info = serviceInfo;
        _setUri = FindSetUriMethod(ConnectorType);

        if (_setUri == null)
        {
            throw new ConnectorException("Unable to find ConnectionFactory.SetUri(), incompatible RabbitMQ assembly");
        }
    }

    internal RabbitMQProviderConnectorFactory()
    {
    }

    public static MethodInfo FindSetUriMethod(Type type)
    {
        TypeInfo typeInfo = type.GetTypeInfo();
        IEnumerable<MethodInfo> declaredMethods = typeInfo.DeclaredMethods;

        foreach (MethodInfo ci in declaredMethods)
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
        string connectionString = CreateConnectionString();
        object result = null;

        if (connectionString != null)
        {
            result = CreateConnection(connectionString);
        }

        if (result == null)
        {
            throw new ConnectorException($"Unable to create instance of '{ConnectorType}'");
        }

        return result;
    }

    public virtual string CreateConnectionString()
    {
        return _configurer.Configure(_info, _config);
    }

    public virtual object CreateConnection(string connectionString)
    {
        object inst = ReflectionHelpers.CreateInstance(ConnectorType);

        if (inst == null)
        {
            return null;
        }

        var uri = new Uri(connectionString, UriKind.Absolute);

        ReflectionHelpers.Invoke(_setUri, inst, new object[]
        {
            uri
        });

        return inst;
    }
}

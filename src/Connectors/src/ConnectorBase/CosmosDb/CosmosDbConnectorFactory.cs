// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.CosmosDb;

public class CosmosDbConnectorFactory
{
    private readonly CosmosDbServiceInfo _info;
    private readonly CosmosDbConnectorOptions _config;
    private readonly CosmosDbProviderConfigurer _configurer = new ();

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
            throw new ConnectorException($"Unable to create instance of '{ConnectorType}'");
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
        return ReflectionHelpers.CreateInstance(ConnectorType, new[] { connectionString, clientOptions });
    }
}

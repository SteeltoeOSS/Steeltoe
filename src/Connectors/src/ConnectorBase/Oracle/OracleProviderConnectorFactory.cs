// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.Oracle;

public class OracleProviderConnectorFactory
{
    private readonly OracleServiceInfo _info;
    private readonly OracleProviderConnectorOptions _config;
    private readonly OracleProviderConfigurer _configurer = new ();

    public OracleProviderConnectorFactory()
    {
    }

    public OracleProviderConnectorFactory(OracleServiceInfo sinfo, OracleProviderConnectorOptions config, Type type)
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

    public virtual object CreateConnection(string connectionString)
    {
        return ReflectionHelpers.CreateInstance(ConnectorType, new object[] { connectionString });
    }
}
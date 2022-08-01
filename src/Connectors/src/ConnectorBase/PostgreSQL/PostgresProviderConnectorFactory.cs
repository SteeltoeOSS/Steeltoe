// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.PostgreSql;

public class PostgresProviderConnectorFactory
{
    private readonly PostgresServiceInfo _info;
    private readonly PostgresProviderConnectorOptions _config;
    private readonly PostgresProviderConfigurer _configurer = new ();
    private readonly Type _type;

    public PostgresProviderConnectorFactory(PostgresServiceInfo serviceInfo, PostgresProviderConnectorOptions options, Type type)
    {
        _info = serviceInfo;
        _config = options ?? throw new ArgumentNullException(nameof(options));
        _type = type;
    }

    internal PostgresProviderConnectorFactory()
    {
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
        return ReflectionHelpers.CreateInstance(_type, new object[] { connectionString });
    }
}

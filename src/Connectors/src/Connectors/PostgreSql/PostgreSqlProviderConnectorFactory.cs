// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.PostgreSql;

public class PostgreSqlProviderConnectorFactory
{
    private readonly PostgreSqlServiceInfo _info;
    private readonly PostgreSqlProviderConnectorOptions _config;
    private readonly PostgreSqlProviderConfigurer _configurer = new();
    private readonly Type _type;

    public PostgreSqlProviderConnectorFactory(PostgreSqlServiceInfo serviceInfo, PostgreSqlProviderConnectorOptions options, Type type)
    {
        ArgumentGuard.NotNull(options);

        _info = serviceInfo;
        _config = options;
        _type = type;
    }

    internal PostgreSqlProviderConnectorFactory()
    {
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
        return ReflectionHelpers.CreateInstance(_type, new object[]
        {
            connectionString
        });
    }
}

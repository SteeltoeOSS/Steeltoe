// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System.Collections.Generic;

namespace Steeltoe.Connector;

/// <summary>
/// Useful for getting connection information from <see cref="IConfiguration"/>
/// </summary>
public class ConnectionStringManager
{
    internal List<IConnectionInfo> _connectionInfos;
    private readonly IConfiguration _configuration;

    public ConnectionStringManager(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionInfos = GetIConnectionTypes();
    }

    /// <summary>
    /// Get connection information of the specified type, optionally from a named service binding
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IConnectionInfo"/> to get</typeparam>
    /// <param name="serviceName">The name of a service binding</param>
    /// <returns><see cref="Connection"/></returns>
    public Connection Get<T>(string serviceName = null)
        where T : IConnectionInfo, new()
    {
        return new T().Get(_configuration, serviceName);
    }

    internal Connection GetByTypeName(string typeName)
    {
        foreach (var t in _connectionInfos)
        {
            if (t.IsSameType(typeName))
            {
                return t.Get(_configuration, string.Empty);
            }
        }

        throw new ConnectorException($"Could not find a matching IConnectionInfo for {typeName}");
    }

    internal Connection GetFromServiceInfo(IServiceInfo serviceInfo)
    {
        foreach (var connectionInfo in _connectionInfos)
        {
            if (connectionInfo.IsSameType(serviceInfo))
            {
                return connectionInfo.Get(_configuration, serviceInfo);
            }
        }

        throw new ConnectorException($"Could not find a matching IConnectionInfo for {serviceInfo.GetType().Name}");
    }

    internal List<IConnectionInfo> GetIConnectionTypes()
    {
        var infos = new List<IConnectionInfo>();
        foreach (var type in ReflectionHelpers.FindInterfacedTypesFromAssemblyAttribute<ConnectionInfoAssemblyAttribute>())
        {
            infos.Add((IConnectionInfo)ReflectionHelpers.CreateInstance(type));
        }

        return infos;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors;

/// <summary>
/// Useful for getting connection information from <see cref="IConfiguration" />.
/// </summary>
public class ConnectionStringManager
{
    private readonly IConfiguration _configuration;
    internal List<IConnectionInfo> ConnectionInfos;

    public ConnectionStringManager(IConfiguration configuration)
    {
        _configuration = configuration;
        ConnectionInfos = GetIConnectionTypes();
    }

    /// <summary>
    /// Get connection information of the specified type, optionally from a named service binding.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="IConnectionInfo" /> to get.
    /// </typeparam>
    /// <param name="serviceName">
    /// The name of a service binding.
    /// </param>
    /// <returns>
    /// <see cref="Connection" />.
    /// </returns>
    public Connection Get<T>(string serviceName = null)
        where T : IConnectionInfo, new()
    {
        return new T().Get(_configuration, serviceName);
    }

    internal Connection GetByTypeName(string typeName)
    {
        foreach (IConnectionInfo t in ConnectionInfos)
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
        foreach (IConnectionInfo connectionInfo in ConnectionInfos)
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

        foreach (Type type in ReflectionHelpers.FindInterfacedTypesFromAssemblyAttribute<ConnectionInfoAssemblyAttribute>())
        {
            infos.Add((IConnectionInfo)ReflectionHelpers.CreateInstance(type));
        }

        return infos;
    }
}

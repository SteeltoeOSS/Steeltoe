// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.Connectors;

/// <summary>
/// Provides access to connectors, whose connection strings originate from merging appsettings.json with cloud service bindings.
/// </summary>
/// <typeparam name="TOptions">
/// The options type, which provides the connection string.
/// </typeparam>
/// <typeparam name="TConnection">
/// The driver-specific connection type.
/// </typeparam>
public sealed class ConnectorFactory<TOptions, TConnection> : IDisposable
    where TOptions : ConnectionStringOptions
    where TConnection : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectorCreateConnection _createConnection;
    private readonly bool _useSingletonConnection;
    private readonly ConcurrentDictionary<string, Connector<TOptions, TConnection>> _namedConnectors = new();

    /// <summary>
    /// Gets the names of the available service bindings.
    /// </summary>
    /// <returns>
    /// The service binding names. An empty string represents the default service binding.
    /// </returns>
    public IReadOnlySet<string> ServiceBindingNames { get; }

    public ConnectorFactory(IServiceProvider serviceProvider, IReadOnlySet<string> serviceBindingNames, ConnectorCreateConnection createConnection,
        bool useSingletonConnection)
    {
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(serviceBindingNames);
        ArgumentGuard.NotNull(createConnection);

        _serviceProvider = serviceProvider;
        ServiceBindingNames = serviceBindingNames;
        _createConnection = createConnection;
        _useSingletonConnection = useSingletonConnection;
    }

    /// <summary>
    /// Gets a connector for the default service binding.
    /// </summary>
    /// <returns>
    /// The connector.
    /// </returns>
    /// <remarks>
    /// This is only available when at most one named service binding exists in the cloud and the client configuration only contains the "Default" entry.
    /// </remarks>
    public Connector<TOptions, TConnection> Get()
    {
        return GetCachedConnector(string.Empty);
    }

    /// <summary>
    /// Gets a connector for the specified service binding name.
    /// </summary>
    /// <param name="serviceBindingName">
    /// The case-sensitive service binding name.
    /// </param>
    /// <returns>
    /// The connector.
    /// </returns>
    public Connector<TOptions, TConnection> Get(string serviceBindingName)
    {
        return GetCachedConnector(serviceBindingName);
    }

    private Connector<TOptions, TConnection> GetCachedConnector(string name)
    {
        // While option values can change at runtime, the list of named options is fixed (determined at application startup).
        return _namedConnectors.GetOrAdd(name, _ => new Connector<TOptions, TConnection>(_serviceProvider, name, _createConnection, _useSingletonConnection));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_useSingletonConnection)
        {
            foreach (Connector<TOptions, TConnection> connector in _namedConnectors.Values)
            {
                connector.Dispose();

                // Don't clear the collection, so that allocated providers will throw when reused after dispose.
            }
        }
    }
}

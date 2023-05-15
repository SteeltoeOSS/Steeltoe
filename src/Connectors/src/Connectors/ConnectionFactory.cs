// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Connectors;

/// <summary>
/// Provides access to named connections, whose connection strings originate from merging appsettings.json with cloud service bindings.
/// </summary>
/// <typeparam name="TOptions">
/// The options type, which provides the connection string.
/// </typeparam>
/// <typeparam name="TConnection">
/// The connection type.
/// </typeparam>
public sealed class ConnectionFactory<TOptions, TConnection> : IDisposable
    where TOptions : ConnectionStringOptions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<TOptions, string, object> _createConnection;
    private readonly bool _useSingletonConnection;
    private readonly ConcurrentDictionary<string, ConnectionProvider<TOptions, TConnection>> _namedConnectionProviders = new();

    private IOptionsMonitor<TOptions> OptionsMonitor => _serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();

    public ConnectionFactory(IServiceProvider serviceProvider, Func<TOptions, string, object> createConnection, bool useSingletonConnection)
    {
        ArgumentGuard.NotNull(createConnection);
        ArgumentGuard.NotNull(serviceProvider);

        _serviceProvider = serviceProvider;
        _createConnection = createConnection;
        _useSingletonConnection = useSingletonConnection;
    }

    /// <summary>
    /// Gets a connection provider for the default service binding. Only use this if a single binding exists.
    /// </summary>
    /// <returns>
    /// The connection provider.
    /// </returns>
    public ConnectionProvider<TOptions, TConnection> GetDefault()
    {
        return GetCachedConnectionProvider(string.Empty);
    }

    /// <summary>
    /// Gets a connection provider for the specified service binding name.
    /// </summary>
    /// <param name="name">
    /// The service binding name.
    /// </param>
    /// <returns>
    /// The connection provider.
    /// </returns>
    public ConnectionProvider<TOptions, TConnection> GetNamed(string name)
    {
        return GetCachedConnectionProvider(name);
    }

    private ConnectionProvider<TOptions, TConnection> GetCachedConnectionProvider(string name)
    {
        return _namedConnectionProviders.GetOrAdd(name,
            _ => new ConnectionProvider<TOptions, TConnection>(OptionsMonitor, name, _createConnection, _useSingletonConnection));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_useSingletonConnection)
        {
            foreach (ConnectionProvider<TOptions, TConnection> connectionProvider in _namedConnectionProviders.Values)
            {
                connectionProvider.Dispose();

                // Don't clear the collection, so that allocated providers will throw when reused after dispose.
            }
        }
    }
}

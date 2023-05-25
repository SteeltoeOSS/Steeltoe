// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<TOptions, string, object> _createConnection;
    private readonly bool _useSingletonConnection;
    private readonly ConcurrentDictionary<string, Connector<TOptions, TConnection>> _namedConnectors = new();

    private IOptionsMonitor<TOptions> OptionsMonitor => _serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();

    /// <summary>
    /// Gets the list of available connector names.
    /// </summary>
    /// <returns>
    /// The connector names. An empty string represents the default connector.
    /// </returns>
    public IReadOnlySet<string> Names { get; }

    public ConnectorFactory(IServiceProvider serviceProvider, IReadOnlySet<string> names, Func<TOptions, string, object> createConnection,
        bool useSingletonConnection)
    {
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNullOrEmpty(names);
        ArgumentGuard.NotNull(createConnection);

        _serviceProvider = serviceProvider;
        Names = names;
        _createConnection = createConnection;
        _useSingletonConnection = useSingletonConnection;
    }

    /// <summary>
    /// Gets a connector for the default service binding. Only use this if a single binding exists.
    /// </summary>
    /// <returns>
    /// The connector.
    /// </returns>
    public Connector<TOptions, TConnection> GetDefault()
    {
        return GetCachedConnector(string.Empty);
    }

    /// <summary>
    /// Gets a connector for the specified service binding name.
    /// </summary>
    /// <param name="name">
    /// The case-sensitive service binding name.
    /// </param>
    /// <returns>
    /// The connector.
    /// </returns>
    public Connector<TOptions, TConnection> GetNamed(string name)
    {
        return GetCachedConnector(name);
    }

    private Connector<TOptions, TConnection> GetCachedConnector(string name)
    {
        // While option values can change at runtime, the list of named options is fixed (determined at application startup).

        if (!Names.Contains(name))
        {
            throw new InvalidOperationException(name == string.Empty ? "Default connector is unavailable." : $"Named connector '{name}' is unavailable.");
        }

        return _namedConnectors.GetOrAdd(name, _ => new Connector<TOptions, TConnection>(OptionsMonitor, name, _createConnection, _useSingletonConnection));
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

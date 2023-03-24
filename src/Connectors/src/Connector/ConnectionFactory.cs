// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Connector;

/// <summary>
/// Provides access to named connections, whose connection strings originate from merging appsettings.json with cloud service bindings.
/// </summary>
/// <typeparam name="TOptions">
/// The options type, which provides the connection string.
/// </typeparam>
/// <typeparam name="TConnection">
/// The connection type.
/// </typeparam>
public sealed class ConnectionFactory<TOptions, TConnection>
    where TOptions : ConnectionStringOptions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<string, object> _createConnection;

    public ConnectionFactory(IServiceProvider serviceProvider, Func<string, object> createConnection)
    {
        ArgumentGuard.NotNull(createConnection);
        ArgumentGuard.NotNull(serviceProvider);

        _serviceProvider = serviceProvider;
        _createConnection = createConnection;
    }

    /// <summary>
    /// Creates a new connection for the default service binding, in case only one binding exists.
    /// </summary>
    /// <returns>
    /// A new connection. Throws when the connection string is unavailable.
    /// </returns>
    public TConnection GetDefaultConnection()
    {
        return GetConnection(string.Empty);
    }

    /// <summary>
    /// Creates a new connection for the specified service binding name.
    /// </summary>
    /// <param name="name">
    /// The service binding name.
    /// </param>
    /// <returns>
    /// A new connection. Throws when the connection string is unavailable.
    /// </returns>
    public TConnection GetConnection(string name)
    {
        string connectionString = GetConnectionString(name);

        if (connectionString == null)
        {
            throw name == string.Empty
                ? new InvalidOperationException("Default connection string not found.")
                : new InvalidOperationException($"Connection string for '{name}' not found.");
        }

        object connection = _createConnection(connectionString);
        return (TConnection)connection;
    }

    /// <summary>
    /// Gets the connection string for the default service binding, in case only one binding exists.
    /// </summary>
    /// <returns>
    /// The connection string, or <c>null</c> if not found.
    /// </returns>
    public string GetDefaultConnectionString()
    {
        return GetConnectionString(string.Empty);
    }

    /// <summary>
    /// Gets the connection string for the specified service binding name.
    /// </summary>
    /// <param name="name">
    /// The service binding name.
    /// </param>
    /// <returns>
    /// The connection string, or <c>null</c> if not found.
    /// </returns>
    public string GetConnectionString(string name)
    {
        var optionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
        TOptions options = optionsMonitor.Get(name);

        return options.ConnectionString;
    }
}

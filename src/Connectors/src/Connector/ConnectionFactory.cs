// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
    /// Gets a connection provider for the default service binding. Only use this if a single binding exists.
    /// </summary>
    /// <returns>
    /// The connection provider.
    /// </returns>
    public ConnectionProvider<TOptions, TConnection> GetDefault()
    {
        return new ConnectionProvider<TOptions, TConnection>(_serviceProvider, string.Empty, _createConnection);
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
        return new ConnectionProvider<TOptions, TConnection>(_serviceProvider, name, _createConnection);
    }
}

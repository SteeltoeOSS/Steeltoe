// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Connector;

public sealed class ConnectionProvider<TOptions, TConnection>
    where TOptions : ConnectionStringOptions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _name;
    private readonly Func<string, object> _createConnection;

    /// <summary>
    /// Gets the options for this service binding.
    /// </summary>
    public TOptions Options
    {
        get
        {
            var optionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return optionsMonitor.Get(_name);
        }
    }

    public ConnectionProvider(IServiceProvider serviceProvider, string name, Func<string, object> createConnection)
    {
        ArgumentGuard.NotNull(createConnection);
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(serviceProvider);

        _serviceProvider = serviceProvider;
        _name = name;
        _createConnection = createConnection;
    }

    /// <summary>
    /// Creates a new connection for this service binding.
    /// </summary>
    /// <returns>
    /// A new connection. Throws when the connection string is unavailable.
    /// </returns>
    public TConnection CreateConnection()
    {
        string connectionString = Options.ConnectionString;

        if (connectionString == null)
        {
            string message = _name == string.Empty
                ? "Connection string for default service binding not found."
                : $"Connection string for service binding '{_name}' not found.";

            throw new InvalidOperationException(message);
        }

        object connection = _createConnection(connectionString);
        return (TConnection)connection;
    }
}

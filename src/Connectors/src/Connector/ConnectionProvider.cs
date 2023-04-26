// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Connector;

public sealed class ConnectionProvider<TOptions, TConnection> : IDisposable
    where TOptions : ConnectionStringOptions
{
    private readonly IOptionsMonitor<TOptions> _optionsMonitor;
    private readonly string _name;
    private readonly Func<TOptions, string, object> _createConnection;
    private readonly Lazy<(object Connection, TOptions OptionsSnapshot)> _singletonConnectionWithOptions;
    private bool _hasDisposedSingleton;

    /// <summary>
    /// Gets the options for this service binding.
    /// </summary>
    public TOptions Options
    {
        get
        {
            if (_singletonConnectionWithOptions != null)
            {
                AssertNotDisposed();

                // Return the options snapshot that was taken at singleton creation time, for consistency.
                // When a singleton connection is used, we don't expose or respond to option changes. We can't dispose
                // the previous singleton, because it may still be in use. And replacing the singleton could result
                // in a single request observing two difference instances, leading to hard-to-reproduce bugs.
                return _singletonConnectionWithOptions.Value.OptionsSnapshot;
            }

            return _optionsMonitor.Get(_name);
        }
    }

    internal ConnectionProvider(IOptionsMonitor<TOptions> optionsMonitor, string name, Func<TOptions, string, object> createConnection,
        bool useSingletonConnection)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(createConnection);

        _optionsMonitor = optionsMonitor;
        _name = name;
        _createConnection = createConnection;

        if (useSingletonConnection)
        {
            _singletonConnectionWithOptions =
                new Lazy<(object Connection, TOptions OptionsSnapshot)>(CreateConnectionFromOptions, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }

    /// <summary>
    /// Creates a new connection for this service binding.
    /// </summary>
    /// <returns>
    /// A new connection. Throws when the connection string is unavailable.
    /// </returns>
    public TConnection CreateConnection()
    {
        if (_singletonConnectionWithOptions != null)
        {
            AssertNotDisposed();
            return (TConnection)_singletonConnectionWithOptions.Value.Connection;
        }

        (object connection, TOptions _) = CreateConnectionFromOptions();
        return (TConnection)connection;
    }

    private void AssertNotDisposed()
    {
        if (_hasDisposedSingleton)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    private (object Connection, TOptions OptionsSnapshot) CreateConnectionFromOptions()
    {
        TOptions optionsSnapshot = _optionsMonitor.Get(_name);
        object connection = _createConnection(optionsSnapshot, _name);

        if (connection == null)
        {
            throw new InvalidOperationException(_name == string.Empty
                ? "Failed to create connection for default service binding."
                : $"Failed to create connection for service binding '{_name}'.");
        }

        return (connection, optionsSnapshot);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_singletonConnectionWithOptions is { IsValueCreated: true, Value.Connection: IDisposable disposable })
        {
            disposable.Dispose();
            _hasDisposedSingleton = true;
        }
    }
}

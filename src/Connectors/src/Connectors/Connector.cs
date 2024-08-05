// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Steeltoe.Connectors;

/// <summary>
/// Provides access to driver-specific options and its connection object for a service binding.
/// </summary>
/// <typeparam name="TOptions">
/// The driver-specific options type.
/// </typeparam>
/// <typeparam name="TConnection">
/// The driver-specific connection type.
/// </typeparam>
public sealed class Connector<TOptions, TConnection> : IDisposable
    where TOptions : ConnectionStringOptions
    where TConnection : class
{
    private readonly string _serviceBindingName;
    private readonly Func<object> _createConnection;
    private readonly bool _useSingletonConnection;
    private readonly IOptionsMonitor<TOptions> _optionsMonitor;

    private readonly object _singletonLock = new();
    private ConnectionWithOptionsSnapshot? _singletonSnapshot;
    private bool _singletonIsDisposed;

    /// <summary>
    /// Gets the options for this service binding.
    /// </summary>
    public TOptions Options
    {
        get
        {
            try
            {
                ConnectionWithOptionsSnapshot? singletonSnapshot = GetOrCreateSingleton();

                if (singletonSnapshot != null)
                {
                    // Return the options snapshot that was taken at singleton creation time, for consistency.
                    // When a singleton connection is used, we don't expose or respond to option changes. We can't dispose
                    // the previous singleton, because it may still be in use. And replacing the singleton could result
                    // in a single request observing two difference instances, leading to hard-to-reproduce bugs.
                    return singletonSnapshot.Options;
                }
            }
            catch (Exception)
            {
                // Don't prevent access to the connection string when the singleton connection fails to create.
            }

            return _optionsMonitor.Get(_serviceBindingName);
        }
    }

    public Connector(IServiceProvider serviceProvider, string serviceBindingName, ConnectorCreateConnection createConnection, bool useSingletonConnection)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(serviceBindingName);
        ArgumentNullException.ThrowIfNull(createConnection);

        _serviceBindingName = serviceBindingName;
        _createConnection = () => createConnection(serviceProvider, serviceBindingName);
        _useSingletonConnection = useSingletonConnection;
        _optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
    }

    /// <summary>
    /// Gets a connection for this service binding. Depending on the connector type, this either creates a new connection or returns a cached instance.
    /// </summary>
    /// <returns>
    /// The connection.
    /// </returns>
    public TConnection GetConnection()
    {
        ConnectionWithOptionsSnapshot? singletonSnapshot = GetOrCreateSingleton();

        if (singletonSnapshot != null)
        {
            return singletonSnapshot.Connection;
        }

        return CreateConnection();
    }

    private ConnectionWithOptionsSnapshot? GetOrCreateSingleton()
    {
        if (_useSingletonConnection)
        {
            // When creation of the singleton fails, we need to retry next time. And we should avoid creating multiple connections in parallel.

            // Because none of the System.Lazy modes suits our needs, we're using a lock here.
            // - ExecutionAndPublication guarantees exclusive access, but caches an initially thrown exception without retry.
            // - PublicationOnly does not cache the exception, but runs connection creation in parallel, discarding any extra instances, which won't be disposed.

            // The retry-after-failure is needed for RabbitMQ and Redis, because their drivers throw when obtaining a connection while RabbitMQ or Redis
            // is initially down. Once we have successfully obtained their connection object, the drivers handle auto-reconnect within that instance.

            lock (_singletonLock)
            {
                // The connection itself is responsible for throwing when used after its disposal.
                // However, there's no point in allocating the connection if we're already disposed.
                ObjectDisposedException.ThrowIf(_singletonIsDisposed, this);

                if (_singletonSnapshot == null)
                {
                    TOptions optionsSnapshot = _optionsMonitor.Get(_serviceBindingName);
                    TConnection connection = CreateConnection();
                    _singletonSnapshot = new ConnectionWithOptionsSnapshot(connection, optionsSnapshot);
                }

                return _singletonSnapshot;
            }
        }

        return null;
    }

    private TConnection CreateConnection()
    {
        object connection = _createConnection();

        if (connection == null)
        {
            throw new InvalidOperationException(_serviceBindingName == string.Empty
                ? "Failed to create connection for default service binding."
                : $"Failed to create connection for service binding '{_serviceBindingName}'.");
        }

        return (TConnection)connection;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_useSingletonConnection)
        {
            IDisposable? connectionToDispose = null;

            lock (_singletonLock)
            {
                if (!_singletonIsDisposed)
                {
                    if (_singletonSnapshot is { Connection: IDisposable disposable })
                    {
                        connectionToDispose = disposable;
                    }

                    _singletonIsDisposed = true;
                }
            }

            connectionToDispose?.Dispose();
        }
    }

    private sealed class ConnectionWithOptionsSnapshot
    {
        public TConnection Connection { get; }
        public TOptions Options { get; }

        public ConnectionWithOptionsSnapshot(TConnection connection, TOptions options)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(options);

            Connection = connection;
            Options = options;
        }
    }
}

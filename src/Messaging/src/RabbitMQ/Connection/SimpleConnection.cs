// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class SimpleConnection : IConnection, RC.NetworkConnection
{
    private readonly int _closeTimeout;
    private readonly ILogger _logger;

    public int LocalPort => Connection.LocalPort;

    public RC.IConnection Connection { get; }

    public bool IsOpen => Connection.IsOpen;

    public string Address => Connection.Endpoint.HostName;

    public int Port => Connection.Endpoint.Port;

    public int RemotePort => Connection.Endpoint.Port;

    public SimpleConnection(RC.IConnection connection, int closeTimeout, ILogger logger = null)
    {
        Connection = connection;
        _closeTimeout = closeTimeout;
        _logger = logger;
    }

    public RC.IModel CreateChannel(bool transactional = false)
    {
        try
        {
            var result = Connection.CreateModel();
            if (result == null)
            {
                throw new RabbitResourceNotAvailableException("The channelMax limit is reached. Try later.");
            }

            if (transactional)
            {
                result.TxSelect();
            }

            return result;
        }
        catch (Exception e)
        {
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    public void Close()
    {
        try
        {
            // _explicitlyClosed = true;

            // let the physical close time out if necessary
            Connection.Close(_closeTimeout);
        }
        catch (AlreadyClosedException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection?.Dispose();
        }
    }

    public void AddBlockedListener(IBlockedListener listener)
    {
        Connection.ConnectionBlocked += listener.HandleBlocked;
        Connection.ConnectionUnblocked += listener.HandleUnblocked;
    }

    public bool RemoveBlockedListener(IBlockedListener listener)
    {
        Connection.ConnectionBlocked -= listener.HandleBlocked;
        Connection.ConnectionUnblocked -= listener.HandleUnblocked;
        return true;
    }
}

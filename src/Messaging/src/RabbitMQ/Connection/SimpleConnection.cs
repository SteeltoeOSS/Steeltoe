// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class SimpleConnection : IConnection, RC.NetworkConnection
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly RC.IConnection _connection;
        private readonly int _closeTimeout;
        private readonly ILogger _logger;

        public int LocalPort => _connection.LocalPort;

        public RC.IConnection Connection => _connection;

        public bool IsOpen => _connection.IsOpen;

        public string Address => _connection.Endpoint.HostName;

        public int Port => _connection.Endpoint.Port;

        public int RemotePort => _connection.Endpoint.Port;

        public SimpleConnection(RC.IConnection connection, int closeTimeout, ILogger logger = null)
        {
            _connection = connection;
            _closeTimeout = closeTimeout;
            _logger = logger;
        }

        public RC.IModel CreateChannel(bool transactional = false)
        {
            try
            {
                var result = _connection.CreateModel();
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
                _connection.Close(_closeTimeout);
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
            _connection.Dispose();
        }

        public void AddBlockedListener(IBlockedListener listener)
        {
            _connection.ConnectionBlocked += listener.HandleBlocked;
            _connection.ConnectionUnblocked += listener.HandleUnblocked;
        }

        public bool RemoveBlockedListener(IBlockedListener listener)
        {
            _connection.ConnectionBlocked -= listener.HandleBlocked;
            _connection.ConnectionUnblocked -= listener.HandleUnblocked;
            return true;
        }
    }
}

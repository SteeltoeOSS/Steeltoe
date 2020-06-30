// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using System;

namespace Steeltoe.Messaging.Rabbit.Connection
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class SimpleConnection : IConnection, NetworkConnection
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly RabbitMQ.Client.IConnection _connection;
        private readonly int _closeTimeout;
        private readonly ILogger _logger;

        public int LocalPort => _connection.LocalPort;

        public RabbitMQ.Client.IConnection Connection => _connection;

        public bool IsOpen => _connection.IsOpen;

        public string Address => _connection.Endpoint.HostName;

        public int Port => _connection.Endpoint.Port;

        public int RemotePort => _connection.Endpoint.Port;

        public SimpleConnection(RabbitMQ.Client.IConnection connection, int closeTimeout, ILogger logger = null)
        {
            _connection = connection;
            _closeTimeout = closeTimeout;
            _logger = logger;
        }

        public IModel CreateChannel(bool transactional = false)
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

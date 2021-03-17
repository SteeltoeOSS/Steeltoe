// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitProxy
    {
        private readonly ILogger<RabbitProxy> _logger;
        private readonly TcpListener _listener = null;
        private volatile bool _run = true;

        public RabbitProxy(ILogger<RabbitProxy> logger)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _logger = new LoggerFactory().CreateLogger<RabbitProxy>();
        }

        public void Start()
        {
            var listenerThread = new Thread(new ParameterizedThreadStart(StartListener));
            listenerThread.Start();
        }

        public int Port
        {
            get => ((IPEndPoint)_listener?.LocalEndpoint)?.Port ?? 0;
        }

        public void Stop()
        {
            _run = false;
            _listener.Stop();
        }

        private void StartListener(object obj)
        {
            try
            {
                while (_run)
                {
                    _logger.LogInformation("Waiting for a connection...");
                    TcpClient client = _listener.AcceptTcpClient();
                    _logger.LogInformation("Connected to client!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleConnection));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                _logger.LogInformation("SocketException: {0}", e);
                _listener.Stop();
            }
        }

        private void HandleConnection(object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();

            var rabbitClient = new TcpClient();
            rabbitClient.Connect("localhost", 5672);
            var rabbitStream = rabbitClient.GetStream();

            byte[] bytes = new byte[1];
            var serverBytes = new byte[1];
            try
            {
                int totalServerBytes = 0;
                int totalClientBytes = 0;

                var t = Task.Run(() =>
                {
                    while (rabbitStream.CanRead)
                    {
                        try
                        {
                            var serverBytesRead = rabbitStream.Read(serverBytes, 0, serverBytes.Length);
                            totalServerBytes += serverBytesRead;

                            if (serverBytesRead != 0 && stream.CanWrite)
                            {
                                stream.Write(serverBytes, 0, serverBytes.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogInformation(ex.Message + ex.StackTrace);
                        }
                    }
                });

                while (stream.CanRead)
                {
                    var clientBytesRead = stream.Read(bytes, 0, bytes.Length);
                    totalClientBytes += clientBytesRead;
                    if (clientBytesRead != 0 && rabbitStream.CanWrite)
                    {
                        rabbitStream.Write(bytes, 0, bytes.Length);
                    }
                }

                t.Wait();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Exception: {0}", e.ToString());
                client.Close();
                rabbitClient.Close();
            }
        }
    }
}

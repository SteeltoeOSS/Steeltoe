// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Stream.Binder.Rabbit;

public class RabbitProxy
{
    private readonly ILogger<RabbitProxy> _logger;
    private readonly TcpListener _listener;
    private volatile bool _run = true;
    private volatile bool _rejectConnections = true;

    public int Port => ((IPEndPoint)_listener?.LocalEndpoint)?.Port ?? 0;

    public RabbitProxy(ILogger<RabbitProxy> logger)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _logger = logger;
        var listenerThread = new Thread(StartListener);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    public void Start()
    {
        _rejectConnections = false;
    }

    public void Stop()
    {
        _run = false;
        _rejectConnections = true;
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

                if (!_rejectConnections)
                {
                    _logger.LogInformation("Connected to client!");
                    var t = new Thread(HandleConnection);
                    t.IsBackground = true;
                    t.Start(client);
                }
                else
                {
                    _logger.LogInformation("Disposing connection");
                    client.Dispose();
                }
            }
        }
        catch (SocketException)
        {
            _listener.Stop();
        }
    }

    private void HandleConnection(object obj)
    {
        var client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        var rabbitClient = new TcpClient();
        rabbitClient.Connect("localhost", 5672);
        NetworkStream rabbitStream = rabbitClient.GetStream();

        rabbitStream.ReadTimeout = 100;
        stream.ReadTimeout = 100;

        byte[] bytes = new byte[1];
        byte[] serverBytes = new byte[1];

        try
        {
            int totalServerBytes = 0;
            int totalClientBytes = 0;

            Task t = Task.Run(() =>
            {
                while (rabbitStream.CanRead && _run)
                {
                    try
                    {
                        int serverBytesRead = rabbitStream.Read(serverBytes, 0, serverBytes.Length);
                        totalServerBytes += serverBytesRead;

                        if (serverBytesRead != 0 && stream.CanWrite)
                        {
                            stream.Write(serverBytes, 0, serverBytes.Length);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            });

            while (stream.CanRead && _run)
            {
                try
                {
                    int clientBytesRead = stream.Read(bytes, 0, bytes.Length);
                    totalClientBytes += clientBytesRead;

                    if (clientBytesRead != 0 && rabbitStream.CanWrite)
                    {
                        rabbitStream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            t.Wait();
        }
        catch (Exception)
        {
            // Ignore
        }
        finally
        {
            client.Close();
            rabbitClient.Close();
            client.Dispose();
            rabbitClient.Dispose();
        }
    }
}

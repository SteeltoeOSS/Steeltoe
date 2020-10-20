using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Steeltoe.Stream.Binder.Rabbit
{
    // Stub for tests that dont depend on broker 
    public class RabbitProxy
    {
        private TcpListener _listener = null;
        private volatile bool _run = true;

        public RabbitProxy()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
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

        private void StartListener(Object obj)
        {
            try
            {
                while (_run)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = _listener.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleConnection));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                _listener.Stop();
            }
        }

        private void HandleConnection(object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();

            byte[] bytes = new byte[256];
            try
            {
                while (stream.Read(bytes, 0, bytes.Length) != 0)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class RabbitOptions
    {
        public const string PREFIX = "spring:rabbitmq";

        public const string DEFAULT_HOST = "localhost";
        public const int DEFAULT_PORT = 5672;
        public const string DEFAULT_USERNAME = "guest";
        public const string DEFAULT_PASSWORD = "guest";

        public RabbitOptions()
        {
            Ssl = new SslOptions();
            Cache = new CacheOptions();
            Listener = new ListenerOptions();
            Template = new TemplateOptions();
        }

        public string Host { get; set; } = DEFAULT_HOST;

        public string DetermineHost()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return Host;
            }

            return parsed[0].Host;
        }

        public int Port { get; set; } = DEFAULT_PORT;

        public int DeterminePort()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return Port;
            }

            return parsed[0].Port;
        }

        public string Addresses { get; set; }

        public string DetermineAddresses()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return null;
            }

            var addressStrings = new List<string>();
            foreach (var parsedAddress in parsed)
            {
                addressStrings.Add(parsedAddress.Host + ":" + parsedAddress.Port);
            }

            return string.Join(',', addressStrings);
        }

        private List<Address> ParsedAddresses
        {
            get
            {
                return ParseAddresses(Addresses);
            }
        }

        private List<Address> ParseAddresses(string addresses)
        {
            var parsedAddresses = new List<Address>();
            if (!string.IsNullOrEmpty(addresses))
            {
                var splitAddresses = addresses.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var address in splitAddresses)
                {
                    var trim = address.Trim();
                    parsedAddresses.Add(new Address(trim));
                }
            }

            return parsedAddresses;
        }

        public string Username { get; set; } = DEFAULT_USERNAME;

        public string DetermineUsername()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return Username;
            }

            var address = parsed[0];
            return address.Username ?? Username;
        }

        public string Password { get; set; } = DEFAULT_PASSWORD;

        public string DeterminePassword()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return Password;
            }

            var address = parsed[0];
            return address.Password ?? Password;
        }

        public SslOptions Ssl { get; set; }

        public string VirtualHost { get; set; }

        public string DetermineVirtualHost()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return VirtualHost;
            }

            var address = parsed[0];
            return address.VirtualHost ?? VirtualHost;
        }

        public bool DetermineSslEnabled()
        {
            var parsed = ParsedAddresses;
            if (parsed.Count == 0)
            {
                return Ssl.Enabled;
            }

            var address = parsed[0];
            return address.SecureConnection ?? Ssl.Enabled;
        }

        public TimeSpan? RequestedHeartbeat { get; set; }

        public bool PublisherConfirms { get; set; }

        public bool PublisherReturns { get; set; }

        public TimeSpan? ConnectionTimeout { get; set; }

        public CacheOptions Cache { get; set; }

        public ListenerOptions Listener { get; set; }

        public TemplateOptions Template { get; set; }

        public class SslOptions
        {
            public bool Enabled { get; set; } = false;

            public bool ValidateServerCertificate { get; set; } = true; // SslPolicyErrors.RemoteCertificateNotAvailable, SslPolicyErrors.RemoteCertificateChainErrors

            public bool VerifyHostname { get; set; } = true;

            public string CertPath { get; set; }

            public string CertPassphrase { get; set; }

            public string ServerHostName { get; set; }

            public SslProtocols Algorithm { get; set; } = SslProtocols.Tls13 | SslProtocols.Tls12;
        }

        public class CacheOptions
        {
            public ChannelOptions Channel { get; set; } = new ChannelOptions();

            public ConnectionOptions Connection { get; set; } = new ConnectionOptions();
        }

        public class ListenerOptions
        {
            public ContainerType Type { get; } = ContainerType.DIRECT;

            public DirectContainerOptions Direct { get; set; } = new DirectContainerOptions();
        }

        public class TemplateOptions
        {
            public RetryOptions Retry { get; set; } = new RetryOptions();

            public bool Mandatory { get; set; } = false;

            public TimeSpan? ReceiveTimeout { get; set; }

            public TimeSpan? ReplyTimeout { get; set; }

            public string Exchange { get; set; } = string.Empty;

            public string RoutingKey { get; set; } = string.Empty;

            public string DefaultReceiveQueue { get; set; }
        }

        public class ChannelOptions
        {
            public int? Size { get; set; }

            public TimeSpan? CheckoutTimeout { get; set; }
        }

        public class ConnectionOptions
        {
            public CachingMode Mode { get; set; } = CachingMode.CHANNEL;

            public int? Size { get; set; }
        }

        public class DirectContainerOptions
        {
            public bool AutoStartup { get; set; } = true;

            public AcknowledgeMode? AcknowledgeMode { get; set; }

            public int? Prefetch { get; set; }

            public bool DefaultRequeueRejected { get; set; } = true;

            public TimeSpan? IdleEventInterval { get; set; }

            public ListenerRetryOptions Retry { get; set; } = new ListenerRetryOptions();

            public bool MissingQueuesFatal { get; set; } = false;

            public int? ConsumersPerQueue { get; set; }

            public bool PossibleAuthenticationFailureFatal { get; set; } = true;
        }

        public class RetryOptions
        {
            public bool Enabled { get; set; } = false;

            public int MaxAttempts { get; set; } = 3;

            public TimeSpan InitialInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

            public double Multiplier { get; set; } = 1.0d;

            public TimeSpan MaxInterval { get; set; } = TimeSpan.FromMilliseconds(10000);
        }

        public class ListenerRetryOptions : RetryOptions
        {
            public bool Stateless { get; set; } = true;
        }

        private class Address
        {
            private const string PREFIX_AMQP = "amqp://";
            private const string PREFIX_AMQP_SECURE = "amqps://";

            public Address(string input)
            {
                input = input.Trim();
                input = TrimPrefix(input);
                input = ParseUsernameAndPassword(input);
                input = ParseVirtualHost(input);
                ParseHostAndPort(input);
            }

            public string Host { get; private set; }

            public int Port { get; private set; }

            public string Username { get; private set; }

            public string Password { get; private set; }

            public string VirtualHost { get; private set; }

            public bool? SecureConnection { get; private set; }

            private string TrimPrefix(string input)
            {
                if (input.StartsWith(PREFIX_AMQP))
                {
                    input = input.Substring(PREFIX_AMQP.Length);
                    SecureConnection = false;
                }
                else if (input.StartsWith(PREFIX_AMQP_SECURE))
                {
                    input = input.Substring(PREFIX_AMQP_SECURE.Length);
                    SecureConnection = true;
                }

                return input;
            }

            private string ParseUsernameAndPassword(string input)
            {
                if (input.Contains("@"))
                {
                    var split = input.Split("@", StringSplitOptions.RemoveEmptyEntries);
                    var creds = split[0];
                    input = split[1];
                    split = creds.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    Username = split[0];
                    if (split.Length > 0)
                    {
                        Password = split[1];
                    }
                }

                return input;
            }

            private string ParseVirtualHost(string input)
            {
                var hostIndex = input.IndexOf('/');
                if (hostIndex >= 0)
                {
                    VirtualHost = input.Substring(hostIndex + 1);
                    if (string.IsNullOrEmpty(VirtualHost))
                    {
                        VirtualHost = "/";
                    }

                    input = input.Substring(0, hostIndex);
                }

                return input;
            }

            private void ParseHostAndPort(string input)
            {
                var portIndex = input.IndexOf(':');
                if (portIndex == -1)
                {
                    Host = input;
                    Port = DEFAULT_PORT;
                }
                else
                {
                    Host = input.Substring(0, portIndex);
                    Port = int.Parse(input.Substring(portIndex + 1));
                }
            }
        }
    }

    public enum ContainerType
    {
        DIRECT
    }
}

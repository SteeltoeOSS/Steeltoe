// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using Steeltoe.Messaging.RabbitMQ.Core;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class RabbitOptions
{
    public const string Prefix = "spring:rabbitmq";

    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5672;
    public const string DefaultUsername = "guest";
    public const string DefaultPassword = "guest";

    private List<Address> ParsedAddresses => ParseAddresses(Addresses);

    public string Host { get; set; } = DefaultHost;

    public int Port { get; set; } = DefaultPort;

    public string Addresses { get; set; }

    public string Username { get; set; } = DefaultUsername;

    public string Password { get; set; } = DefaultPassword;

    public SslOptions Ssl { get; set; }

    public string VirtualHost { get; set; }

    public TimeSpan? RequestedHeartbeat { get; set; }

    public bool PublisherConfirms { get; set; }

    public bool PublisherReturns { get; set; }

    public TimeSpan? ConnectionTimeout { get; set; }

    public CacheOptions Cache { get; set; }

    public ListenerOptions Listener { get; set; }

    public TemplateOptions Template { get; set; }

    public RabbitOptions()
    {
        Ssl = new SslOptions();
        Cache = new CacheOptions();
        Listener = new ListenerOptions();
        Template = new TemplateOptions();
    }

    public string DetermineHost()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return Host;
        }

        return parsed[0].Host;
    }

    public int DeterminePort()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return Port;
        }

        return parsed[0].Port;
    }

    public string DetermineAddresses()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return null;
        }

        var addressStrings = new List<string>();

        foreach (Address parsedAddress in parsed)
        {
            addressStrings.Add($"{parsedAddress.Host}:{parsedAddress.Port}");
        }

        return string.Join(',', addressStrings);
    }

    private List<Address> ParseAddresses(string addresses)
    {
        var parsedAddresses = new List<Address>();

        if (!string.IsNullOrEmpty(addresses))
        {
            string[] splitAddresses = addresses.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string address in splitAddresses)
            {
                string trim = address.Trim();
                parsedAddresses.Add(new Address(trim));
            }
        }

        return parsedAddresses;
    }

    public string DetermineUsername()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return Username;
        }

        Address address = parsed[0];
        return address.Username ?? Username;
    }

    public string DeterminePassword()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return Password;
        }

        Address address = parsed[0];
        return address.Password ?? Password;
    }

    public string DetermineVirtualHost()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return VirtualHost;
        }

        Address address = parsed[0];
        return address.VirtualHost ?? VirtualHost;
    }

    public bool DetermineSslEnabled()
    {
        List<Address> parsed = ParsedAddresses;

        if (parsed.Count == 0)
        {
            return Ssl.Enabled;
        }

        Address address = parsed[0];
        return address.SecureConnection ?? Ssl.Enabled;
    }

    public class SslOptions
    {
        public bool Enabled { get; set; }

        public bool ValidateServerCertificate { get; set; } =
            true; // SslPolicyErrors.RemoteCertificateNotAvailable, SslPolicyErrors.RemoteCertificateChainErrors

        public bool VerifyHostname { get; set; } = true;

        public string CertPath { get; set; }

        public string CertPassPhrase { get; set; }

        public string ServerHostName { get; set; }

        public SslProtocols Algorithm { get; set; } = SslProtocols.Tls13 | SslProtocols.Tls12;
    }

    public class CacheOptions
    {
        public ChannelOptions Channel { get; set; } = new();

        public ConnectionOptions Connection { get; set; } = new();
    }

    public class ListenerOptions
    {
        public ContainerType Type { get; } = ContainerType.Direct;

        public DirectContainerOptions Direct { get; set; } = new();
    }

    public class TemplateOptions
    {
        public RetryOptions Retry { get; set; } = new();

        public bool Mandatory { get; set; }

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
        public CachingMode Mode { get; set; } = CachingMode.Channel;

        public int? Size { get; set; }
    }

    public class DirectContainerOptions
    {
        public bool AutoStartup { get; set; } = true;

        public AcknowledgeMode? AcknowledgeMode { get; set; }

        public int? Prefetch { get; set; }

        public bool DefaultRequeueRejected { get; set; } = true;

        public TimeSpan? IdleEventInterval { get; set; }

        public ListenerRetryOptions Retry { get; set; } = new();

        public bool MissingQueuesFatal { get; set; }

        public int? ConsumersPerQueue { get; set; }

        public bool PossibleAuthenticationFailureFatal { get; set; } = true;
    }

    public class RetryOptions
    {
        public bool Enabled { get; set; }

        public int MaxAttempts { get; set; } = 3;

        public TimeSpan InitialInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

        public double Multiplier { get; set; } = 1.0d;

        public TimeSpan MaxInterval { get; set; } = TimeSpan.FromMilliseconds(10000);
    }

    public class ListenerRetryOptions : RetryOptions
    {
        public bool Stateless { get; set; } = true;
    }

    private sealed class Address
    {
        private const string PrefixAmqp = "amqp://";
        private const string PrefixAmqpSecure = "amqps://";

        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Username { get; private set; }

        public string Password { get; private set; }

        public string VirtualHost { get; private set; }

        public bool? SecureConnection { get; private set; }

        public Address(string input)
        {
            input = input.Trim();
            input = TrimPrefix(input);
            input = ParseUsernameAndPassword(input);
            input = ParseVirtualHost(input);
            ParseHostAndPort(input);
        }

        private string TrimPrefix(string input)
        {
            if (input.StartsWith(PrefixAmqp))
            {
                input = input.Substring(PrefixAmqp.Length);
                SecureConnection = false;
            }
            else if (input.StartsWith(PrefixAmqpSecure))
            {
                input = input.Substring(PrefixAmqpSecure.Length);
                SecureConnection = true;
            }

            return input;
        }

        private string ParseUsernameAndPassword(string input)
        {
            if (input.Contains("@"))
            {
                string[] split = input.Split("@", StringSplitOptions.RemoveEmptyEntries);
                string credentials = split[0];
                input = split[1];
                split = credentials.Split(":", StringSplitOptions.RemoveEmptyEntries);
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
            int hostIndex = input.IndexOf('/');

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
            int portIndex = input.IndexOf(':');

            if (portIndex == -1)
            {
                Host = input;
                Port = DefaultPort;
            }
            else
            {
                Host = input.Substring(0, portIndex);
                Port = int.Parse(input.Substring(portIndex + 1));
            }
        }
    }
}

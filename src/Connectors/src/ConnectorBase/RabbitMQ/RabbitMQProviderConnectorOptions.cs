// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.RabbitMQ
{
    public class RabbitMQProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Scheme = "amqp";
        public const string Default_SSLScheme = "amqps";
        public const string Default_Server = "127.0.0.1";
        public const int Default_Port = 5672;
        public const int Default_SSLPort = 5671;
        private const string RABBITMQ_CLIENT_SECTION_PREFIX = "rabbitmq:client";
        private const string RABBIT_CLIENT_SECTION_PREFIX = "rabbit:client";

        public RabbitMQProviderConnectorOptions()
        {
        }

        public RabbitMQProviderConnectorOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(RABBIT_CLIENT_SECTION_PREFIX);
            section.Bind(this);

            var sectionMQ = config.GetSection(RABBITMQ_CLIENT_SECTION_PREFIX);
            sectionMQ.Bind(this);
        }

        public bool SslEnabled { get; set; } = false;

        public string Uri { get; set; }

        public string Server { get; set; } = Default_Server;

        public int Port { get; set; } = Default_Port;

        public int SslPort { get; set; } = Default_SSLPort;

        public string Username { get; set; }

        public string Password { get; set; }

        public string VirtualHost { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Uri))
            {
                return Uri;
            }

            var uri = SslEnabled
                ? new UriInfo(Default_SSLScheme, Server, SslPort, Username, Password, VirtualHost)
                : new UriInfo(Default_Scheme, Server, Port, Username, Password, VirtualHost);

            return uri.ToString();
        }
    }
}

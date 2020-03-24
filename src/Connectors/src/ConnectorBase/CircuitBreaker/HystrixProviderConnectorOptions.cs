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

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.Hystrix
{
    public class HystrixProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Scheme = "amqp";
        public const string Default_SSLScheme = "amqps";
        public const string Default_Server = "127.0.0.1";
        public const int Default_Port = 5672;
        public const int Default_SSLPort = 5671;
        private const string HYSTRIX_CLIENT_SECTION_PREFIX = "hystrix:client";

        public HystrixProviderConnectorOptions()
        {
        }

        public HystrixProviderConnectorOptions(IConfiguration config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(HYSTRIX_CLIENT_SECTION_PREFIX);
            section.Bind(this);
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

            UriInfo uri = null;
            if (SslEnabled)
            {
                uri = new UriInfo(Default_SSLScheme, Server, SslPort, Username, Password, VirtualHost);
            }
            else
            {
                uri = new UriInfo(Default_Scheme, Server, Port, Username, Password, VirtualHost);
            }

            return uri.ToString();
        }
    }
}

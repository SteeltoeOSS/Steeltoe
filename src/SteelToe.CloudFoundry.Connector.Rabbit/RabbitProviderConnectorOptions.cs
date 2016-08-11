//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//


using Microsoft.Extensions.Configuration;
using System;
using SteelToe.CloudFoundry.Connector.Services;

namespace SteelToe.CloudFoundry.Connector.Rabbit
{
    public class RabbitProviderConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Server = "127.0.0.1";
        public const int Default_Port = 5672;
        private const string RABBIT_CLIENT_SECTION_PREFIX = "rabbit:client";

        public RabbitProviderConnectorOptions()
        {
        }

        public RabbitProviderConnectorOptions(IConfiguration config) :
            base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            var section = config.GetSection(RABBIT_CLIENT_SECTION_PREFIX);
            section.Bind(this);
        }

        public string Uri { get; set; }
        public string Server { get; set; } = Default_Server;
        public int Port { get; set; } = Default_Port;
        public string Username { get; set; }
        public string Password { get; set;  }
        public string VirtualHost { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Uri)) {
                return Uri;
            }

            UriInfo uri = new UriInfo("amqp", Server, Port, Username, Password, VirtualHost);
            return uri.ToString();
        }

    }
}

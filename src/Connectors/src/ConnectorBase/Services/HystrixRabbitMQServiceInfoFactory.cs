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

using Steeltoe.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    public class HystrixRabbitMQServiceInfoFactory : ServiceInfoFactory
    {
        public static readonly Tags HYSTRIX_RABBIT_SERVICE_TAGS = new Tags("hystrix-amqp");

        private static readonly string[] _scheme = new string[] { RabbitMQServiceInfo.AMQP_SCHEME, RabbitMQServiceInfo.AMQPS_SCHEME };

        public HystrixRabbitMQServiceInfoFactory()
            : base(HYSTRIX_RABBIT_SERVICE_TAGS, _scheme)
        {
        }

        public override bool Accept(Service binding)
        {
            return TagsMatch(binding) && UriCredentialsMatchesScheme(binding.Credentials);
        }

        public override IServiceInfo Create(Service binding)
        {
            var amqpCredentials = binding.Credentials["amqp"];
            var uri = GetUriFromCredentials(amqpCredentials);
            var sslEnabled = GetBoolFromCredentials(amqpCredentials, "ssl");
            if (amqpCredentials.ContainsKey("uris"))
            {
                var uris = GetListFromCredentials(amqpCredentials, "uris");
                return new HystrixRabbitMQServiceInfo(binding.Name, uri, uris, sslEnabled);
            }

            return new HystrixRabbitMQServiceInfo(binding.Name, uri, sslEnabled);
        }

        private bool UriCredentialsMatchesScheme(Dictionary<string, Credential> credentials)
        {
            if (credentials.ContainsKey("amqp"))
            {
                var amqpDict = credentials["amqp"];
                var uri = GetStringFromCredentials(amqpDict, UriKeys);
                if (uri != null)
                {
                    foreach (var uriScheme in UriSchemes)
                    {
                        if (uri.StartsWith(uriScheme + "://"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

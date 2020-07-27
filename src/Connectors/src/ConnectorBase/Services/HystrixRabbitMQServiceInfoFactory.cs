// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class HystrixRabbitMQServiceInfoFactory : ServiceInfoFactory
    {
        public static readonly Tags HYSTRIX_RABBIT_SERVICE_TAGS = new Tags("hystrix-amqp");

        private static string[] _scheme = new string[] { RabbitMQServiceInfo.AMQP_SCHEME, RabbitMQServiceInfo.AMQPS_SCHEME };

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

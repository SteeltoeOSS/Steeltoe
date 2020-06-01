// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RabbitMQServiceInfoFactory : ServiceInfoFactory
    {
        public static readonly Tags RABBIT_SERVICE_TAGS = new Tags("rabbit");

        private static string[] _scheme = new string[] { RabbitMQServiceInfo.AMQP_SCHEME, RabbitMQServiceInfo.AMQPS_SCHEME };

        public RabbitMQServiceInfoFactory()
            : base(RABBIT_SERVICE_TAGS, _scheme)
        {
        }

        public override bool Accept(Service binding)
        {
            bool result = base.Accept(binding);
            if (result)
            {
                result = !HystrixRabbitMQServiceInfoFactory.HYSTRIX_RABBIT_SERVICE_TAGS.ContainsOne(binding.Tags);
            }

            return result;
        }

        public override IServiceInfo Create(Service binding)
        {
            string uri = GetUriFromCredentials(binding.Credentials);
            string managementUri = GetStringFromCredentials(binding.Credentials, "http_api_uri");

            if (binding.Credentials.ContainsKey("uris"))
            {
                List<string> uris = GetListFromCredentials(binding.Credentials, "uris");
                List<string> managementUris = GetListFromCredentials(binding.Credentials, "http_api_uris");
                return new RabbitMQServiceInfo(binding.Name, uri, managementUri, uris, managementUris);
            }

            return new RabbitMQServiceInfo(binding.Name, uri, managementUri);
        }
    }
}

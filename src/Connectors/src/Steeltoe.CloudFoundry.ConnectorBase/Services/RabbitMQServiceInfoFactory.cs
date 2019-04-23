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

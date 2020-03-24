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

namespace Steeltoe.Connector.Services
{
    public class EurekaServiceInfoFactory : ServiceInfoFactory
    {
        public EurekaServiceInfoFactory()
            : base(new Tags("eureka"), System.Array.Empty<string>())
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            var uri = GetUriFromCredentials(binding.Credentials);
            var clientId = GetClientIdFromCredentials(binding.Credentials);
            var clientSecret = GetClientSecretFromCredentials(binding.Credentials);
            var accessTokenUri = GetAccessTokenUriFromCredentials(binding.Credentials);

            return new EurekaServiceInfo(binding.Name, uri, clientId, clientSecret, accessTokenUri);
        }
    }
}

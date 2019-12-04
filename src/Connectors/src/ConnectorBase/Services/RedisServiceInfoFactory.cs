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

using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RedisServiceInfoFactory : ServiceInfoFactory
    {
        public RedisServiceInfoFactory()
            : base(new Tags("redis"), new string[] { RedisServiceInfo.REDIS_SCHEME, RedisServiceInfo.REDIS_SECURE_SCHEME })
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            var uri = GetUriFromCredentials(binding.Credentials);
            if (string.IsNullOrEmpty(uri))
            {
                var host = GetHostFromCredentials(binding.Credentials);
                var password = GetPasswordFromCredentials(binding.Credentials);
                var port = GetPortFromCredentials(binding.Credentials);
                var tlsPort = GetTlsPortFromCredentials(binding.Credentials);
                var tlsEnabled = tlsPort != 0;

                return new RedisServiceInfo(
                        binding.Name,
                        tlsEnabled ? RedisServiceInfo.REDIS_SECURE_SCHEME : RedisServiceInfo.REDIS_SCHEME,
                        host,
                        tlsEnabled ? tlsPort : port,
                        password);
            }
            else
            {
                return new RedisServiceInfo(binding.Name, uri);
            }
        }
    }
}

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

using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class SsoServiceInfoFactory : ServiceInfoFactory
    {
        public SsoServiceInfoFactory()
            : base(new Tags("p-identity"), "uaa")
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            string clientId = GetClientIdFromCredentials(binding.Credentials);
            string clientSecret = GetClientSecretFromCredentials(binding.Credentials);
            string authDomain = GetStringFromCredentials(binding.Credentials, "auth_domain");
            string uri = GetUriFromCredentials(binding.Credentials);

            if (!string.IsNullOrEmpty(authDomain))
            {
                return new SsoServiceInfo(binding.Name, clientId, clientSecret, authDomain);
            }

            if (!string.IsNullOrEmpty(uri))
            {
                return new SsoServiceInfo(binding.Name, clientId, clientSecret, UpdateUaaScheme(uri));
            }

            return null;
        }

        internal string UpdateUaaScheme(string uaaString)
        {
            if (uaaString.StartsWith("uaa:"))
            {
                return "https:" + uaaString.Substring(4);
            }

            return uaaString;
        }
    }
}

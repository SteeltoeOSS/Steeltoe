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

using System;
using SteelToe.Extensions.Configuration.CloudFoundry;

namespace SteelToe.CloudFoundry.Connector.Services
{
    public abstract class RelationalServiceInfoFactory : ServiceInfoFactory
    {
        public RelationalServiceInfoFactory(Tags tags, string scheme) :
            base(tags, scheme)
        {

        }
        public RelationalServiceInfoFactory(Tags tags, string[] schemes) :
            base(tags, schemes)
        {

        }

        public override IServiceInfo Create(Service binding)
        {
            string uri = GetUriFromCredentials(binding.Credentials);
            if (uri == null)
            {
                String host = GetHostFromCredentials(binding.Credentials);
                int port = GetPortFromCredentials(binding.Credentials);

                String username = GetUsernameFromCredentials(binding.Credentials);
                String password = GetPasswordFromCredentials(binding.Credentials);

                String database = GetStringFromCredentials(binding.Credentials, "name");

                if (host != null)
                {
                    uri = new UriInfo(DefaultUriScheme, host, port, username, password, database).ToString();
                }
            }
            return Create(binding.Name, uri);
        }

        public abstract IServiceInfo Create(string id, string url);
    }
}

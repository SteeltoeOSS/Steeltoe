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

using Steeltoe.CloudFoundry.Connector.Services;
using Microsoft.Extensions.Options;

namespace Steeltoe.CloudFoundry.Connector.OAuth
{
    public class OAuthConnectorFactory
    {
        private SsoServiceInfo _info;
        private OAuthConnectorOptions _config;
        private OAuthConfigurer _configurer = new OAuthConfigurer();

        public OAuthConnectorFactory(SsoServiceInfo sinfo, OAuthConnectorOptions config)
        {
            _info = sinfo;
            _config = config;
        }
        internal IOptions<OAuthServiceOptions> Create(IServiceProvider provider)
        {
            var opts = _configurer.Configure(_info, _config);
            return opts;
        }
    }
}

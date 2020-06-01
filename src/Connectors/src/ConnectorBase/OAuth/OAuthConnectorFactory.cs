// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

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

        public IOptions<OAuthServiceOptions> Create(IServiceProvider provider)
        {
            var opts = _configurer.Configure(_info, _config);
            return opts;
        }
    }
}

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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace SteelToe.CloudFoundry.Connector.OAuth
{
    public class OAuthConnectorOptions : AbstractServiceConnectorOptions
    {
        private const string SECURITY_CLIENT_SECTION_PREFIX = "security:oauth2:client";
        private const string SECURITY_RESOURCE_SECTION_PREFIX = "security:oauth2:resource";

        internal const string Default_AuthorizationUri = "/oauth/authorize";
        internal const string Default_AccessTokenUri = "/oauth/token";
        internal const string Default_UserInfoUri = "/userinfo";
        internal const string Default_CheckTokenUri = "/check_token";
        internal const string Default_JwtTokenKey = "/token_key";
        internal const string Default_OAuthServiceUrl = "Default_OAuthServiceUrl";
        internal const string Default_ClientId = "Default_ClientId";
        internal const string Default_ClientSecret = "Default_ClientSecret";

        public OAuthConnectorOptions()
        {
        }

        public OAuthConnectorOptions(IConfiguration config) :
            base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(SECURITY_CLIENT_SECTION_PREFIX);
            section.Bind(this);

            section = config.GetSection(SECURITY_RESOURCE_SECTION_PREFIX);
            section.Bind(this);

        }

        public string OAuthServiceUrl { get; set; } = Default_OAuthServiceUrl;
        public string ClientId { get; set; } = Default_ClientId;
        public string ClientSecret { get; set; } = Default_ClientSecret;
        public string UserAuthorizationUri { get; set; } = Default_AuthorizationUri;
        public string AccessTokenUri { get; set; } = Default_AccessTokenUri;
        public string UserInfoUri { get; set; } = Default_UserInfoUri;
        public string TokenInfoUri { get; set; } = Default_CheckTokenUri;
        public string JwtKeyUri { get; set; } = Default_JwtTokenKey;
        public List<string> Scope { get; set; }

    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelToe.CloudFoundry.Connector.OAuth
{
    public class OAuthConnectorDefaults
    {

        public const string Default_AuthorizationUri = "/oauth/authorize";
        public const string Default_AccessTokenUri = "/oauth/token";
        public const string Default_UserInfoUri = "/userinfo";
        public const string Default_CheckTokenUri = "/check_token";
        public const string Default_JwtTokenKey = "/token_keys";
        public const string Default_OAuthServiceUrl = "Default_OAuthServiceUrl";
        public const string Default_ClientId = "Default_ClientId";
        public const string Default_ClientSecret = "Default_ClientSecret";
        public const bool Default_ValidateCertificates = true;
    }
}

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

using SteelToe.CloudFoundry.Connector.Services;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.OAuth.Test
{


    public class OAuthConfigurerTest
    {

        [Fact]
        public void Update_WithDefaultConnectorOptions_UpdatesOAuthOptions_AsExpected()
        {
            OAuthServiceOptions opts = new OAuthServiceOptions();
            OAuthConnectorOptions config = new OAuthConnectorOptions();
            OAuthConfigurer configurer = new OAuthConfigurer();
            configurer.UpdateOptions(config, opts);

            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal(OAuthConnectorOptions.Default_ClientId, opts.ClientId);
            Assert.Equal(OAuthConnectorOptions.Default_ClientSecret, opts.ClientSecret);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }

        [Fact]
        public void Update_WithServiceInfo_UpdatesOAuthOptions_AsExpected()
        {
            OAuthServiceOptions opts = new OAuthServiceOptions();
            SsoServiceInfo si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "http://foo.bar");
     
            OAuthConfigurer configurer = new OAuthConfigurer();
            configurer.UpdateOptions(si, opts);
            

            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal("myClientId", opts.ClientId);
            Assert.Equal("myClientSecret", opts.ClientSecret);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            OAuthConnectorOptions config = new OAuthConnectorOptions();
            OAuthConfigurer configurer = new OAuthConfigurer();
            var result = configurer.Configure(null, config);

            Assert.NotNull(result);
            var opts = result.Value;
            Assert.NotNull(opts);

            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal(OAuthConnectorOptions.Default_ClientId, opts.ClientId);
            Assert.Equal(OAuthConnectorOptions.Default_ClientSecret, opts.ClientSecret);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal(OAuthConnectorOptions.Default_OAuthServiceUrl + OAuthConnectorOptions.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            SsoServiceInfo si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "http://foo.bar");
            OAuthConnectorOptions config = new OAuthConnectorOptions();
            OAuthConfigurer configurer = new OAuthConfigurer();
            var result = configurer.Configure(si, config);

            Assert.NotNull(result);
            var opts = result.Value;
            Assert.NotNull(opts);


            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal("myClientId", opts.ClientId);
            Assert.Equal("myClientSecret", opts.ClientSecret);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal("http://foo.bar" + OAuthConnectorOptions.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }
    }
}

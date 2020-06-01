// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.OAuth.Test
{
    public class OAuthConfigurerTest
    {
        [Fact]
        public void Update_WithDefaultConnectorOptions_UpdatesOAuthOptions_AsExpected()
        {
            OAuthServiceOptions opts = new OAuthServiceOptions();
            OAuthConnectorOptions config = new OAuthConnectorOptions()
            {
                ValidateCertificates = false
            };
            OAuthConfigurer configurer = new OAuthConfigurer();
            configurer.UpdateOptions(config, opts);

            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_ClientId, opts.ClientId);
            Assert.Equal(OAuthConnectorDefaults.Default_ClientSecret, opts.ClientSecret);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.False(opts.ValidateCertificates);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }

        [Fact]
        public void Update_WithServiceInfo_UpdatesOAuthOptions_AsExpected()
        {
            OAuthServiceOptions opts = new OAuthServiceOptions();
            SsoServiceInfo si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");

            OAuthConfigurer configurer = new OAuthConfigurer();
            configurer.UpdateOptions(si, opts);

            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal("myClientId", opts.ClientId);
            Assert.Equal("myClientSecret", opts.ClientSecret);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.True(opts.ValidateCertificates);
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

            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_ClientId, opts.ClientId);
            Assert.Equal(OAuthConnectorDefaults.Default_ClientSecret, opts.ClientSecret);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal(OAuthConnectorDefaults.Default_OAuthServiceUrl + OAuthConnectorDefaults.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.True(opts.ValidateCertificates);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            SsoServiceInfo si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");
            OAuthConnectorOptions config = new OAuthConnectorOptions();
            OAuthConfigurer configurer = new OAuthConfigurer();
            var result = configurer.Configure(si, config);

            Assert.NotNull(result);
            var opts = result.Value;
            Assert.NotNull(opts);

            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal("myClientId", opts.ClientId);
            Assert.Equal("myClientSecret", opts.ClientSecret);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.True(opts.ValidateCertificates);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }
    }
}

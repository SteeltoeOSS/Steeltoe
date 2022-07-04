// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.ConfigServer.Test;
using System.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test;

public class ConfigServerClientSettingsOptionsTest
{
    [Fact]
    public void ConfigureConfigServerClientSettingsOptions_WithDefaults()
    {
        var services = new ServiceCollection().AddOptions();
        var environment = HostingHelpers.GetHostingEnvironment("Production");

        var builder = new ConfigurationBuilder().AddConfigServer(environment);
        services.AddSingleton<IConfiguration>(_ => builder.Build());

        services.ConfigureConfigServerClientOptions();
        var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
        Assert.NotNull(service);
        var options = service.Value;
        Assert.NotNull(options);
        TestHelper.VerifyDefaults(options.Settings);
    }

    [Fact]
    public void ConfigureConfigServerClientSettingsOptions_WithValues()
    {
        var services = new ServiceCollection().AddOptions();
        var appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"": ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""headers"" : {
                                ""foo"":""bar"",
                                ""bar"":""foo""
                            },
                            ""health"": {
                                ""enabled"": true
                            },
                            ""failfast"": ""true""
                        }
                      }
                    }
                }";
        using var sandbox = new Sandbox();
        var path = sandbox.CreateFile("appsettings.json", appsettings);
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);
        builder.AddJsonFile(fileName);
        var config = builder.Build();
        services.AddSingleton<IConfiguration>(config);

        services.ConfigureConfigServerClientOptions();
        var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
        Assert.NotNull(service);
        var options = service.Value;
        Assert.NotNull(options);

        Assert.Equal(ConfigServerClientSettings.DefaultProviderEnabled, options.Enabled);
        Assert.True(options.FailFast);
        Assert.Equal(ConfigServerClientSettings.DefaultUri, options.Uri);
        Assert.Equal("development", options.Environment);
        Assert.Equal(ConfigServerClientSettings.DefaultAccessTokenUri, options.AccessTokenUri);
        Assert.Equal(ConfigServerClientSettings.DefaultClientId, options.ClientId);
        Assert.Equal(ConfigServerClientSettings.DefaultClientSecret, options.ClientSecret);
        Assert.Equal(ConfigServerClientSettings.DefaultCertificateValidation, options.ValidateCertificates);
        Assert.Equal(ConfigServerClientSettings.DefaultInitialRetryInterval, options.RetryInitialInterval);
        Assert.Equal(ConfigServerClientSettings.DefaultMaxRetryAttempts, options.RetryAttempts);
        Assert.Equal(ConfigServerClientSettings.DefaultRetryEnabled, options.RetryEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultRetryMultiplier, options.RetryMultiplier);
        Assert.Equal(ConfigServerClientSettings.DefaultMaxRetryInterval, options.RetryMaxInterval);
        Assert.Equal(ConfigServerClientSettings.DefaultTimeoutMilliseconds, options.Timeout);
        Assert.Equal(ConfigServerClientSettings.DefaultVaultTokenRenewRate, options.TokenRenewRate);
        Assert.Equal(ConfigServerClientSettings.DefaultVaultTokenTtl, options.TokenTtl);
        Assert.Equal(ConfigServerClientSettings.DefaultDiscoveryEnabled, options.DiscoveryEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultConfigserverServiceid, options.DiscoveryServiceId);
        Assert.Null(options.Name);
        Assert.Null(options.Label);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Null(options.Token);
        Assert.NotNull(options.Headers);
        Assert.Equal("foo", options.Headers["bar"]);
        Assert.Equal("bar", options.Headers["foo"]);

        var settings = options.Settings;
        Assert.NotNull(settings);

        Assert.Equal(ConfigServerClientSettings.DefaultProviderEnabled, settings.Enabled);
        Assert.True(settings.FailFast);
        Assert.Equal(ConfigServerClientSettings.DefaultUri, settings.Uri);
        Assert.Equal("development", settings.Environment);
        Assert.Equal(ConfigServerClientSettings.DefaultAccessTokenUri, settings.AccessTokenUri);
        Assert.Equal(ConfigServerClientSettings.DefaultClientId, settings.ClientId);
        Assert.Equal(ConfigServerClientSettings.DefaultClientSecret, settings.ClientSecret);
        Assert.Equal(ConfigServerClientSettings.DefaultCertificateValidation, settings.ValidateCertificates);
        Assert.Equal(ConfigServerClientSettings.DefaultInitialRetryInterval, settings.RetryInitialInterval);
        Assert.Equal(ConfigServerClientSettings.DefaultMaxRetryAttempts, settings.RetryAttempts);
        Assert.Equal(ConfigServerClientSettings.DefaultRetryEnabled, settings.RetryEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultRetryMultiplier, settings.RetryMultiplier);
        Assert.Equal(ConfigServerClientSettings.DefaultMaxRetryInterval, settings.RetryMaxInterval);
        Assert.Equal(ConfigServerClientSettings.DefaultTimeoutMilliseconds, settings.Timeout);
        Assert.Equal(ConfigServerClientSettings.DefaultVaultTokenRenewRate, settings.TokenRenewRate);
        Assert.Equal(ConfigServerClientSettings.DefaultVaultTokenTtl, settings.TokenTtl);
        Assert.Equal(ConfigServerClientSettings.DefaultDiscoveryEnabled, settings.DiscoveryEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultConfigserverServiceid, settings.DiscoveryServiceId);
        Assert.Equal(ConfigServerClientSettings.DefaultHealthEnabled, settings.HealthEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultHealthTimetolive, settings.HealthTimeToLive);
        Assert.Null(settings.Name);
        Assert.Null(settings.Label);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
        Assert.Null(settings.Token);
        Assert.NotNull(settings.Headers);
        Assert.Equal("foo", options.Headers["bar"]);
        Assert.Equal("bar", options.Headers["foo"]);
    }
}

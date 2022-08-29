// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public static class TestHelper
{
    public static void VerifyDefaults(ConfigServerClientSettings settings)
    {
        Assert.Equal(ConfigServerClientSettings.DefaultProviderEnabled, settings.Enabled);
        Assert.Equal(ConfigServerClientSettings.DefaultFailFast, settings.FailFast);
        Assert.Equal(ConfigServerClientSettings.DefaultUri, settings.Uri);
        Assert.Equal(ConfigServerClientSettings.DefaultEnvironment, settings.Environment);
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
        Assert.Equal(ConfigServerClientSettings.DefaultConfigserverServiceId, settings.DiscoveryServiceId);
        Assert.Equal(ConfigServerClientSettings.DefaultHealthEnabled, settings.HealthEnabled);
        Assert.Equal(ConfigServerClientSettings.DefaultHealthTimeToLive, settings.HealthTimeToLive);

        try
        {
            Assert.Null(settings.Name);
        }
        catch
        {
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, settings.Name);
        }

        Assert.Null(settings.Label);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
        Assert.Null(settings.Token);
        Assert.Empty(settings.Headers);
    }
}

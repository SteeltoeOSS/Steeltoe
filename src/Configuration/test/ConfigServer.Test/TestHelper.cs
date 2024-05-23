// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal static class TestHelper
{
    public static void VerifyDefaults(ConfigServerClientSettings settings)
    {
        Assert.True(settings.Enabled);
        Assert.False(settings.FailFast);
        Assert.Equal("http://localhost:8888", settings.Uri);
        Assert.Equal("Production", settings.Environment);
        Assert.Null(settings.AccessTokenUri);
        Assert.Null(settings.ClientId);
        Assert.Null(settings.ClientSecret);
        Assert.True(settings.ValidateCertificates);
        Assert.Equal(1000, settings.RetryInitialInterval);
        Assert.Equal(6, settings.RetryAttempts);
        Assert.False(settings.RetryEnabled);
        Assert.Equal(1.1, settings.RetryMultiplier);
        Assert.Equal(2000, settings.RetryMaxInterval);
        Assert.Equal(60_000, settings.Timeout);
        Assert.Equal(60_000, settings.TokenRenewRate);
        Assert.Equal(300_000, settings.TokenTtl);
        Assert.False(settings.DiscoveryEnabled);
        Assert.Equal("configserver", settings.DiscoveryServiceId);
        Assert.True(settings.HealthEnabled);
        Assert.Equal(300_000, settings.HealthTimeToLive);

        if (settings.Name != null)
        {
            Assert.Equal(HostingHelpers.TestAppName, settings.Name);
        }

        Assert.Null(settings.Label);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
        Assert.Null(settings.Token);
        Assert.Empty(settings.Headers);
    }
}

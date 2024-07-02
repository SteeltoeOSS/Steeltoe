// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;

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
        Assert.Equal(1000, settings.Retry.InitialInterval);
        Assert.Equal(6, settings.Retry.Attempts);
        Assert.False(settings.Retry.Enabled);
        Assert.Equal(1.1, settings.Retry.Multiplier);
        Assert.Equal(2000, settings.Retry.MaxInterval);
        Assert.Equal(60_000, settings.Timeout);
        Assert.Equal(60_000, settings.TokenRenewRate);
        Assert.Equal(300_000, settings.TokenTtl);
        Assert.False(settings.Discovery.Enabled);
        Assert.Equal("configserver", settings.Discovery.ServiceId);
        Assert.True(settings.Health.Enabled);
        Assert.Equal(300_000, settings.Health.TimeToLive);

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

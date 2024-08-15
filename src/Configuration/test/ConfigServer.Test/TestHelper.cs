// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal static class TestHelper
{
    public static void VerifyDefaults(ConfigServerClientOptions options)
    {
        Assert.True(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("http://localhost:8888", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
        Assert.True(options.ValidateCertificates);
        Assert.Equal(1000, options.Retry.InitialInterval);
        Assert.Equal(6, options.Retry.MaxAttempts);
        Assert.False(options.Retry.Enabled);
        Assert.Equal(1.1, options.Retry.Multiplier);
        Assert.Equal(2000, options.Retry.MaxInterval);
        Assert.Equal(60_000, options.Timeout);
        Assert.Equal(60_000, options.TokenRenewRate);
        Assert.Equal(300_000, options.TokenTtl);
        Assert.False(options.Discovery.Enabled);
        Assert.Equal("configserver", options.Discovery.ServiceId);
        Assert.True(options.Health.Enabled);
        Assert.Equal(300_000, options.Health.TimeToLive);

        if (options.Name != null)
        {
            Assert.Equal(TestHostEnvironmentFactory.TestAppName, options.Name);
        }

        Assert.Null(options.Label);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Null(options.Token);
        Assert.Empty(options.Headers);
    }
}

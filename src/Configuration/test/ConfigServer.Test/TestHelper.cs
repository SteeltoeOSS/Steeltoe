// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer.Test;

internal static class TestHelper
{
    public static void VerifyDefaults(ConfigServerClientOptions options, string? expectedAppName)
    {
        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("http://localhost:8888");
        options.Environment.Should().Be("Production");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ValidateCertificates.Should().BeTrue();
        options.Retry.InitialInterval.Should().Be(1000);
        options.Retry.MaxAttempts.Should().Be(6);
        options.Retry.Enabled.Should().BeFalse();
        options.Retry.Multiplier.Should().Be(1.1);
        options.Retry.MaxInterval.Should().Be(2000);
        options.Timeout.Should().Be(60_000);
        options.TokenRenewRate.Should().Be(60_000);
        options.TokenTtl.Should().Be(300_000);
        options.Discovery.Enabled.Should().BeFalse();
        options.Discovery.ServiceId.Should().Be("configserver");
        options.Health.Enabled.Should().BeTrue();
        options.Health.TimeToLive.Should().Be(300_000);
        options.Name.Should().Be(expectedAppName);
        options.Label.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.Headers.Should().BeEmpty();
    }
}

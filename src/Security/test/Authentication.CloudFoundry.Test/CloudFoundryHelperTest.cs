// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryHelperTest
{
    [Fact]
    public void GetBackChannelHandler_ReturnsExpected()
    {
        HttpMessageHandler result1 = CloudFoundryHelper.GetBackChannelHandler(false);
        Assert.NotNull(result1);

        HttpMessageHandler result2 = CloudFoundryHelper.GetBackChannelHandler(true);
        Assert.Null(result2);
    }

    [Fact]
    public void GetTokenValidationParameters_ReturnsExpected()
    {
        TokenValidationParameters parameters = CloudFoundryHelper.GetTokenValidationParameters(null, "https://foo.bar.com/keyurl", null, false);
        Assert.False(parameters.ValidateAudience, "Audience validation should not be enabled by default");
        Assert.True(parameters.ValidateIssuer, "Issuer validation should be enabled by default");
        Assert.NotNull(parameters.IssuerValidator);
        Assert.True(parameters.ValidateLifetime, "Token lifetime validation should be enabled by default");
        Assert.NotNull(parameters.IssuerSigningKeyResolver);
    }

    [Fact]
    public void GetExpTime_FindsTime()
    {
        string info = TestHelpers.GetValidTokenInfoRequestResponse();
        JsonElement payload = JsonDocument.Parse(info).RootElement;
        DateTime dateTime = CloudFoundryHelper.GetExpTime(payload);
        Assert.Equal(new DateTime(2016, 9, 2, 8, 04, 23, DateTimeKind.Utc), dateTime);
    }

    [Fact]
    public void GetIssueTime_FindsTime()
    {
        string info = TestHelpers.GetValidTokenInfoRequestResponse();
        JsonElement payload = JsonDocument.Parse(info).RootElement;
        DateTime dateTime = CloudFoundryHelper.GetIssueTime(payload);
        Assert.Equal(new DateTime(2016, 9, 1, 20, 04, 23, DateTimeKind.Utc), dateTime);
    }

    [Fact]
    public void GetScopes_FindsScopes()
    {
        string info = TestHelpers.GetValidTokenInfoRequestResponse();
        JsonElement payload = JsonDocument.Parse(info).RootElement;
        List<string> scopes = CloudFoundryHelper.GetScopes(payload);
        Assert.Contains("openid", scopes);
        Assert.Single(scopes);
    }
}

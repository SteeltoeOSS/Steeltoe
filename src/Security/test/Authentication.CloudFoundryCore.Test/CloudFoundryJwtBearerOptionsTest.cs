// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryJwtBearerOptionsTest
{
    private const string DefaultJwtTokenUrl =
        $"http://{CloudFoundryDefaults.OAuthServiceUrl}{CloudFoundryDefaults.JwtTokenUri}";

    public static TheoryData<string, string> SetEndpointsData()
    {
        var data = new TheoryData<string, string>();
        var newDomain = "http://not-the-original-domain";

        data.Add(string.Empty, DefaultJwtTokenUrl);
        data.Add("   ", DefaultJwtTokenUrl);
        data.Add(default, DefaultJwtTokenUrl);
        data.Add(newDomain, newDomain + CloudFoundryDefaults.JwtTokenUri);

        return data;
    }

    [Fact]
    public void DefaultConstructor_SetsUpDefaultOptions()
    {
        var opts = new CloudFoundryJwtBearerOptions();

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal(DefaultJwtTokenUrl, opts.JwtKeyUrl);
        Assert.True(opts.SaveToken);
    }

    [Theory]
    [MemberData(nameof(SetEndpointsData))]
    public void SetEndpoints_WithNewDomain_ReturnsExpected(string newDomain, string expectedUrl)
    {
        var options = new CloudFoundryJwtBearerOptions();

        options.SetEndpoints(newDomain);

        Assert.Equal(expectedUrl, options.JwtKeyUrl);
    }
}

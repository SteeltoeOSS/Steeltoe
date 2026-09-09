// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;

namespace Steeltoe.Common.Test.Extensions;

public sealed class MaskedUriTest
{
    [Fact]
    public void MaskSingleBasicAuthentication()
    {
        const string source = "http://username:password@www.example.com/";
        const string expected = "http://****:****@www.example.com/";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void MaskMultiBasicAuthentication()
    {
        const string source = "http://username:password@www.example.com/,http://user2:pass2@www.other.com/";
        const string expected = "http://****:****@www.example.com/,http://****:****@www.other.com/";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void DoNotMaskIfNoBasicAuthentication()
    {
        const string source = "http://www.example.com/";
        const string expected = "http://www.example.com/";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("pwd")]
    [InlineData("api-key")]
    [InlineData("apikey")]
    [InlineData("access_token")]
    [InlineData("bearer_token")]
    [InlineData("client_secret")]
    [InlineData("auth_code")]
    [InlineData("credentials")]
    [InlineData("sig")]
    [InlineData("signature")]
    [InlineData("hash")]
    [InlineData("pin")]
    [InlineData("otp")]
    [InlineData("nonce")]
    [InlineData("certificate")]
    [InlineData("TOKEN")]
    public void MaskSensitiveQueryStringParameter(string parameterName)
    {
        string source = $"http://www.example.com/?{parameterName}=abc123";
        string expected = $"http://www.example.com/?{parameterName}=****";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void DoNotMaskNonSensitiveQueryStringParameter()
    {
        const string source = "http://www.example.com/?name=value&page=1";
        const string expected = "http://www.example.com/?name=value&page=1";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void MaskOnlySensitiveQueryStringParametersAmongMultiple()
    {
        const string source = "http://www.example.com/?name=value&access_token=abc123&page=1";
        const string expected = "http://www.example.com/?name=value&access_token=****&page=1";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void MaskBasicAuthenticationAndSensitiveQueryStringParameter()
    {
        const string source = "http://username:password@www.example.com/?token=abc123";
        const string expected = "http://****:****@www.example.com/?token=****";

        MaskedUri uri = new Uri(source);

        uri.ToString().Should().Be(expected);
    }
}

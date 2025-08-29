// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryTokenValidatorTest
{
    [Fact]
    public void ValidateIssuer_ValidatesCorrectly()
    {
        var cftv = new CloudFoundryTokenValidator();

        var uaaResult = cftv.ValidateIssuer("https://uaa.system.testcloud.com/", null, null);
        var foobarResult = cftv.ValidateIssuer("https://foobar.system.testcloud.com/", null, null);

        Assert.NotNull(uaaResult);
        Assert.Null(foobarResult);
    }

    [Fact]
    public void ValidateAudience_ValidatesFromAuthServerOptionsCorrectly()
    {
        var cftv = new CloudFoundryTokenValidator(new AuthServerOptions
        {
            ClientId = "test-client",
            AdditionalAudiences = new[] { "additional-audience" }
        });
        var audiences = new[] { "profile", "some-api", "additional-audience" };
        var result = cftv.ValidateAudience(audiences, null, null);
        Assert.True(result);

        audiences = new[] { "invalid-audience" };
        result = cftv.ValidateAudience(audiences, null, null);
        Assert.False(result);
    }

    [Fact]
    public void ValidateAudience_ValidatesFromTokenValidationParameters()
    {
        var cftv = new CloudFoundryTokenValidator();
        var audiences = new[] { "profile", "some-api", "additional-audience" };
        var validationParametersSingleAudience = new TokenValidationParameters { ValidAudience = "some-api" };
        var result = cftv.ValidateAudience(audiences, null, validationParametersSingleAudience);
        Assert.True(result, "Valid from single audience in TokenValidationParameters");

        var validationParametersListOfAudiences = new TokenValidationParameters
        {
            ValidAudiences = new[] { "some-api" }
        };
        result = cftv.ValidateAudience(audiences, null, validationParametersListOfAudiences);
        Assert.True(result, "Valid from audience list in TokenValidationParameters");

        audiences = new[] { "invalid-audience" };
        result = cftv.ValidateAudience(audiences, null, validationParametersSingleAudience);
        Assert.False(result, "Invalid from single audience in TokenValidationParameters");
        result = cftv.ValidateAudience(audiences, null, validationParametersSingleAudience);
        Assert.False(result, "Invalid from audience list in TokenValidationParameters");
    }
}
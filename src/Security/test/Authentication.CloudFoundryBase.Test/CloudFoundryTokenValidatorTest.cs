// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryTokenValidatorTest
{
    [Fact]
    public void ValidateIssuer_ValidatesCorrectly()
    {
        var validator = new CloudFoundryTokenValidator();

        string uaaResult = validator.ValidateIssuer("https://uaa.system.testcloud.com/", null, null);
        string foobarResult = validator.ValidateIssuer("https://foobar.system.testcloud.com/", null, null);

        Assert.NotNull(uaaResult);
        Assert.Null(foobarResult);
    }
}

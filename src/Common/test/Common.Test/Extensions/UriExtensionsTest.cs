// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;

namespace Steeltoe.Common.Test.Extensions;

public sealed class UriExtensionsTest
{
    [Fact]
    public void TryGetUsernamePassword_AllowsColonInPassword()
    {
        var uri = new Uri("http://username:pass::word@host.com");

        bool result = uri.TryGetUsernamePassword(out string? username, out string? password);

        result.Should().BeTrue();
        username.Should().Be("username");
        password.Should().Be("pass::word");
    }

    [Theory]
    [InlineData("http://user+name:pass+word@host.com", "user+name", "pass+word")]
    [InlineData("http://user%2Bname:pass%2Bword@host.com", "user+name", "pass+word")]
    [InlineData("http://user%20name:pass%20word@host.com", "user name", "pass word")]
    [InlineData("http://user:p%C3%A4ss@host.com", "user", "p\u00E4ss")]
    public void TryGetUsernamePassword_DecodesAccordingToUriRules(string uriString, string expectedUsername, string expectedPassword)
    {
        var uri = new Uri(uriString);

        bool result = uri.TryGetUsernamePassword(out string? username, out string? password);

        result.Should().BeTrue();
        username.Should().Be(expectedUsername);
        password.Should().Be(expectedPassword);
    }
}

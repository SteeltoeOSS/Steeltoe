// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Test;

public sealed class SecurityUtilitiesTest
{
    [Fact]
    public void SanitizeInput_ReturnsNullAndEmptyUnchanged()
    {
        SecurityUtilities.SanitizeInput(null).Should().BeNull();
        SecurityUtilities.SanitizeInput(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void SanitizeInput_EncodesHtml()
    {
        SecurityUtilities.SanitizeInput(">some string<").Should().Be("&gt;some string&lt;");
    }

    [Fact]
    public void SanitizeInput_RemovesCrlf()
    {
        SecurityUtilities.SanitizeInput("some\rparagraph\rwith\rcarriage\rreturns").Should().NotContain("\r");
        SecurityUtilities.SanitizeInput("some\nparagraph\nwith\nline\nendings").Should().NotContain("\n");
    }

    [Fact]
    public void SanitizeForLogging_ReturnsNullAndEmptyUnchanged()
    {
        SecurityUtilities.SanitizeForLogging(null).Should().BeNull();
        SecurityUtilities.SanitizeForLogging(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForLogging_RemovesControlCharacters()
    {
        SecurityUtilities.SanitizeForLogging("test\x00string\x1F").Should().Be("teststring");
        SecurityUtilities.SanitizeForLogging("test\tstring").Should().Be("test\tstring"); // Tab should be preserved
    }

    [Fact]
    public void SanitizeForLogging_KeepsPrintableAscii()
    {
        SecurityUtilities.SanitizeForLogging("ABC123!@#$%^&*()").Should().Be("ABC123!@#$%^&*()");
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,<script>alert(1)</script>", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("not-a-url", false)]
    public void IsUrlSafe_ValidatesUrlsCorrectly(string? url, bool expected)
    {
        SecurityUtilities.IsUrlSafe(url).Should().Be(expected);
    }
}

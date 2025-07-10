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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Test;

public class SecurityUtilitiesTest
{
    [Fact]
    public void SanitizeInput_ReturnsNullAndEmptyUnchanged()
    {
        Assert.Null(SecurityUtilities.SanitizeInput(null));
        Assert.Equal(string.Empty, SecurityUtilities.SanitizeInput(string.Empty));
    }

    [Fact]
    public void SanitizeInput_EncodesHtml()
    {
        Assert.Equal("&gt;some string&lt;", SecurityUtilities.SanitizeInput(">some string<"));
    }

    [Fact]
    public void SanitizeInput_RemovesCrlf()
    {
        Assert.DoesNotContain("\r", SecurityUtilities.SanitizeInput("some\rparagraph\rwith\rcarriage\rreturns"));
        Assert.DoesNotContain("\n", SecurityUtilities.SanitizeInput("some\nparagraph\nwith\nline\nendings"));
    }
}
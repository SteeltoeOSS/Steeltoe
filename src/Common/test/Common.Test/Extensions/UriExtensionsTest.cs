// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;
using System;
using Xunit;

namespace Steeltoe.Common.Test.Extensions;

public class UriExtensionsTest
{
    [Fact]
    public void MaskExistingBasicAuthenticationToString()
    {
        var uri = new Uri("http://username:password@www.example.com/");
        var expected = "http://****:****@www.example.com/";

        var masked = uri.ToMaskedString();

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void MaskExistingBasicAuthentication()
    {
        var uri = new Uri("http://username:password@www.example.com/");
        var expected = new Uri("http://****:****@www.example.com/");

        var masked = uri.ToMaskedUri();

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void DontMaskStringIfNotBasicAuthenticationExists()
    {
        var uri = new Uri("http://www.example.com/");
        var expected = uri.ToString();

        var masked = uri.ToMaskedString();

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void DontMaskUriIfNotBasicAuthenticationExists()
    {
        var uri = new Uri("http://www.example.com/");
        var expected = new Uri(uri.ToString());

        var masked = uri.ToMaskedUri();

        Assert.Equal(expected, masked);
    }
}

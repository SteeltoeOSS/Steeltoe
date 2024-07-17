// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;

namespace Steeltoe.Common.Test.Extensions;

public sealed class UriExtensionsTest
{
    [Fact]
    public void MaskSingleBasicAuthentication()
    {
        var uri = new Uri("http://username:password@www.example.com/");
        const string expected = "http://****:****@www.example.com/";

        string masked = uri.ToMaskedString();

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void MaskMultiBasicAuthentication()
    {
        var uri = new Uri("http://username:password@www.example.com/,http://user2:pass2@www.other.com/");
        const string expected = "http://****:****@www.example.com/,http://****:****@www.other.com/";

        string masked = uri.ToMaskedString();

        Assert.Equal(expected, masked);
    }

    [Fact]
    public void DoNotMaskIfNoBasicAuthentication()
    {
        var uri = new Uri("http://www.example.com/");
        string expected = uri.ToString();

        string masked = uri.ToMaskedString();

        Assert.Equal(expected, masked);
    }
}

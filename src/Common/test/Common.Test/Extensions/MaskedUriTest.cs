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
}

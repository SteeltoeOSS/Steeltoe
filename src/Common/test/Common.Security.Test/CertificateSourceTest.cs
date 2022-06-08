// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Security.Test;

public class CertificateSourceTest
{
    [Fact]
    public void CertificateSource_HasOptionsConfigurer()
    {
        Assert.NotNull(new CertificateSource("somePath").OptionsConfigurer);
    }
}

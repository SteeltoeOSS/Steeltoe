// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class PemCertificateSourceTest
    {
        [Fact]
        public void PemCertificateSource_HasOptionsConfigurer()
        {
            Assert.NotNull(new PemCertificateSource("somePath", "somePath").OptionsConfigurer);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class PemConfigureCertificateOptionsTest
    {
        [Fact]
        public void AddPemFiles_ReadsFiles_CreatesCertificate()
        {
            var config = new ConfigurationBuilder()
                .AddPemFiles("instance.crt", "instance.key")
                .Build();
            Assert.NotNull(config["certificate"]);
            Assert.NotNull(config["privateKey"]);
            var pemConfig = new PemConfigureCertificateOptions(config);
            var opts = new CertificateOptions();
            pemConfig.Configure(opts);
            Assert.NotNull(opts.Certificate);
            Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, opts.Name);
            Assert.True(opts.Certificate.HasPrivateKey);
        }
    }
}

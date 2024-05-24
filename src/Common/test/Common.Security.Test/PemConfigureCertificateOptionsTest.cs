// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class PemConfigureCertificateOptionsTest
{
    [Fact]
    public void AddPemFiles_ReadsFiles_CreatesCertificate()
    {
        string certificateName = "test";
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(certificateName, "instance.crt", "instance.key").Build();
        configurationRoot[$"clientCertificates:{certificateName}:certificate"].Should().NotBeNull();
        configurationRoot[$"clientCertificates:{certificateName}:privateKey"].Should().NotBeNull();
        var pemConfig = new PemConfigureCertificateOptions(configurationRoot);

        var opts = new CertificateOptions
        {
            Name = certificateName
        };

        pemConfig.Configure(certificateName, opts);
        opts.Certificate.Should().NotBeNull();
        certificateName.Should().BeEquivalentTo(opts.Name);
        opts.Certificate.HasPrivateKey.Should().BeTrue();
    }
}

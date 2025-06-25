// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Certificates.Test;

public sealed class CertificateConfigurationExtensionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void AddCertificate_SetsPaths()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(CertificateName, "instance.crt", "instance.key").Build();
        configurationRoot[$"Certificates:{CertificateName}:certificateFilePath"].Should().Be("instance.crt");
        configurationRoot[$"Certificates:{CertificateName}:privateKeyFilePath"].Should().Be("instance.key");
    }
}

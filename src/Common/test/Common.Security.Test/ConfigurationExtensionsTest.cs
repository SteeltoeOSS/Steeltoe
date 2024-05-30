// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class ConfigurationExtensionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void AddCertificate_SetsPaths()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(CertificateName, "instance.crt", "instance.key").Build();
        configurationRoot[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath"].Should().Be("instance.crt");
        configurationRoot[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:privateKeyFilePath"].Should().Be("instance.key");
    }
}

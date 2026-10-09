// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Certificates.Test;

public sealed class CertificateConfigurationExtensionsTest
{
    private const string ConfigurationKeyPrefix = $"Certificates:{CertificateConfigurationExtensions.AppInstanceIdentityCertificateName}:";

    [Fact]
    public void AddAppInstanceIdentityCertificate_SetsPaths_RunningLocal()
    {
        var builder = new ConfigurationBuilder();
        builder.AddAppInstanceIdentityCertificate();
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot[ConfigurationKeyPrefix + "certificateFilePath"].Should().EndWith($"{LocalCertificateWriter.CertificateFilenamePrefix}Cert.pem");
        configurationRoot[ConfigurationKeyPrefix + "privateKeyFilePath"].Should().EndWith($"{LocalCertificateWriter.CertificateFilenamePrefix}Key.pem");
    }

    [Fact]
    public void AddAppInstanceIdentityCertificate_SetsPaths_RunningOnCloudFoundry()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certificateScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var privateKeyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        var builder = new ConfigurationBuilder();
        builder.AddAppInstanceIdentityCertificate();
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot[ConfigurationKeyPrefix + "certificateFilePath"].Should().Be("instance.crt");
        configurationRoot[ConfigurationKeyPrefix + "privateKeyFilePath"].Should().Be("instance.key");
    }
}

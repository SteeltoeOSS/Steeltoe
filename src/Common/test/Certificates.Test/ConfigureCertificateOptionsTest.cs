// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Certificates.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_NoPath_NoCertificate(string certificateName)
    {
        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_BadPath_NoCertificate(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "does-not-exist.crt"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [TheorySkippedOnPlatform(nameof(OSPlatform.OSX))]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnEmptyFile(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "empty.crt"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action configureAction = () => configureOptions.Configure(certificateName, options);
        configureAction.Should().ThrowExactly<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "invalid.key"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action configureAction = () => configureOptions.Configure(certificateName, options);
        configureAction.Should().ThrowExactly<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificateWithChain(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.p12"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();
        using var collector = new CertificateCollector();

        configureOptions.Configure(certificateName, options);
        collector.Collect(options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
        options.IssuerChain.Should().HaveCount(1);
        X509Certificate2 issuer = options.IssuerChain[0];
        issuer.Subject.Should().Be("CN=Diego Instance Identity Intermediate CA");
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificateWithChain(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "instance.key"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();
        using var collector = new CertificateCollector();

        configureOptions.Configure(certificateName, options);
        collector.Collect(options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
        options.IssuerChain.Should().HaveCount(1);
        X509Certificate2 issuer = options.IssuerChain[0];
        issuer.Subject.Should().Be("CN=Diego Instance Identity Intermediate CA");
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }
}

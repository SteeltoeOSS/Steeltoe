// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Certificates.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_NoPath_NoCertificate(string certificateName)
    {
        var configureOptions = new ConfigureCertificateOptions(new ConfigurationBuilder().Build());
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_BadPath_NoCertificate(string certificateName)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "does-not-exist.crt"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);

        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_EmptyFile_Crashes(string certificateName)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "empty.crt"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action action = () => configureOptions.Configure(certificateName, options);
        action.Should().Throw<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey(string certificateName)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "invalid.key"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Assert.Throws<CryptographicException>(() => configureOptions.Configure(certificateName, options));

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificate(string certificateName)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(certificateName, "instance.p12").Build();
        configurationRoot[$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"].Should().NotBeNull();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificate(string certificateName)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(certificateName, "instance.crt", "instance.key").Build();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptionsUpdateOnFileContentChange(string certificateName)
    {
        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string secondCertificateContent = await File.ReadAllTextAsync("instance2.crt", TestContext.Current.CancellationToken);
        string secondPrivateKeyContent = await File.ReadAllTextAsync("instance2.key", TestContext.Current.CancellationToken);
        var secondX509 = X509Certificate2.CreateFromPemFile("instance2.crt", "instance2.key");
        string certificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstCertificateContent);
        string privateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstPrivateKeyContent);

        if (TestContext.Current.IsRunningOnBuildServer())
        {
            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCertificate(certificateName, certificateFilePath, privateKeyFilePath);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();

        optionsMonitor.Get(certificateName).Certificate.Should().BeEquivalentTo(firstX509);

        await File.WriteAllTextAsync(certificateFilePath, secondCertificateContent, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(privateKeyFilePath, secondPrivateKeyContent, TestContext.Current.CancellationToken);

        SpinWait.SpinUntil(() =>
        {
            try
            {
                return optionsMonitor.Get(certificateName).Certificate!.Equals(secondX509);
            }
            catch
            {
                return false; // File(s) may not be readable yet. Swallow exceptions and keep spinning
            }
        }, 4.Seconds());

        optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptionsUpdateOnFileLocationChange(string certificateName)
    {
        using var sandbox = new Sandbox();
        string instance1Certificate = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string instance1PrivateKey = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string instance2Certificate = await File.ReadAllTextAsync("instance2.crt", TestContext.Current.CancellationToken);
        string instance2PrivateKey = await File.ReadAllTextAsync("instance2.key", TestContext.Current.CancellationToken);
        var secondX509 = X509Certificate2.CreateFromPemFile("instance2.crt", "instance2.key");
        string certificate1FilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", instance1Certificate);
        string privateKey1FilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", instance1PrivateKey);
        string certificate2FilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", instance2Certificate);
        string privateKey2FilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", instance2PrivateKey);

        if (TestContext.Current.IsRunningOnBuildServer())
        {
            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCertificate(certificateName, certificate1FilePath, privateKey1FilePath);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(certificateName).Certificate.Should().BeEquivalentTo(firstX509);

        IOptionsChangeTokenSource<CertificateOptions>[] tokenSources = [.. serviceProvider.GetServices<IOptionsChangeTokenSource<CertificateOptions>>()];

        tokenSources.OfType<FilePathInOptionsChangeTokenSource<CertificateOptions>>().Should().HaveCount(2);
        IChangeToken changeToken = tokenSources.OfType<ConfigurationChangeTokenSource<CertificateOptions>>().Single().GetChangeToken();

        bool changeCalled = false;

        using (changeToken.RegisterChangeCallback(_ => changeCalled = true, "changed-path"))
        {
            changeCalled.Should().BeFalse("nothing has changed yet");
            configurationRoot[$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = certificate2FilePath;
            configurationRoot[$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = privateKey2FilePath;
            configurationRoot.Reload();
            changeCalled.Should().BeTrue("file path information changed");
            optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
        }

        changeCalled = false;
        changeToken = tokenSources.OfType<FilePathInOptionsChangeTokenSource<CertificateOptions>>().First().GetChangeToken();

        using (changeToken.RegisterChangeCallback(_ => changeCalled = true, "original-content-in-new-path"))
        {
            changeCalled.Should().BeFalse("nothing has changed yet");
            await File.WriteAllTextAsync(certificate2FilePath, instance1Certificate, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(privateKey2FilePath, instance1PrivateKey, TestContext.Current.CancellationToken);

            SpinWait.SpinUntil(() =>
            {
                try
                {
                    return optionsMonitor.Get(certificateName).Certificate!.Equals(firstX509);
                }
                catch
                {
                    return false; // File(s) may not be readable yet. Swallow exceptions and keep spinning
                }
            }, 6.Seconds());

            changeCalled.Should().BeTrue("file contents changed");
            optionsMonitor.Get(certificateName).Certificate.Should().Be(firstX509);
        }
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Certificates.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void ConfigureCertificateOptions_NoPath_NoCertificate()
    {
        var configureOptions = new ConfigureCertificateOptions(new ConfigurationBuilder().Build());
        var options = new CertificateOptions();

        configureOptions.Configure(CertificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Fact]
    public void ConfigureCertificateOptions_BadPath_NoCertificate()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { $"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath", "doesnotexist.crt" }
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configuration);

        var options = new CertificateOptions();

        configureOptions.Configure(CertificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Fact]
    public void ConfigureCertificateOptions_EmptyFile_Crashes()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { $"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath", "empty.crt" }
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action action = () => configureOptions.Configure(CertificateName, options);
        action.Should().Throw<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Fact]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { $"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath", "instance.crt" },
            { $"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:privateKeyFilePath", "invalid.key" }
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Assert.Throws<CryptographicException>(() => configureOptions.Configure(CertificateName, options));

        options.Certificate.Should().BeNull();
    }

    [Fact]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(CertificateName, "instance.p12").Build();
        configurationRoot[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath"].Should().NotBeNull();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(CertificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(CertificateName, "instance.crt", "instance.key").Build();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(CertificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public async Task CertificateOptionsUpdateOnFileContentChange()
    {
        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt");
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key");
        var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string secondCertificateContent = await File.ReadAllTextAsync("instance2.crt");
        string secondPrivateKeyContent = await File.ReadAllTextAsync("instance2.key");
        var secondX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string certificateFilePath = sandbox.CreateFile("cert", firstCertificateContent);
        string privateKeyFilePath = sandbox.CreateFile("key", firstPrivateKeyContent);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCertificate(CertificateName, certificateFilePath, privateKeyFilePath);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.ConfigureCertificateOptions(Options.DefaultName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();

        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(firstX509);

        await File.WriteAllTextAsync(certificateFilePath, secondCertificateContent);
        await File.WriteAllTextAsync(privateKeyFilePath, secondPrivateKeyContent);
        await Task.Delay(2000);

        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(secondX509);
    }

    [Fact]
    public async Task CertificateOptionsUpdateOnFileLocationChange()
    {
        using var sandbox = new Sandbox();
        string instance1Certificate = await File.ReadAllTextAsync("instance.crt");
        string instance1PrivateKey = await File.ReadAllTextAsync("instance.key");
        var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string instance2Certificate = await File.ReadAllTextAsync("instance2.crt");
        string instance2PrivateKey = await File.ReadAllTextAsync("instance2.key");
        var secondX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string certificate1FilePath = sandbox.CreateFile("cert", instance1Certificate);
        string privateKey1FilePath = sandbox.CreateFile("key", instance1PrivateKey);
        string certificate2FilePath = sandbox.CreateFile("cert2", instance2Certificate);
        string privateKey2FilePath = sandbox.CreateFile("key2", instance2PrivateKey);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCertificate(CertificateName, certificate1FilePath, privateKey1FilePath);
        IConfigurationRoot configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.ConfigureCertificateOptions(CertificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(firstX509);

        IOptionsChangeTokenSource<CertificateOptions>[] tokenSources = serviceProvider.GetServices<IOptionsChangeTokenSource<CertificateOptions>>().ToArray();
        tokenSources.OfType<FilePathInOptionsChangeTokenSource<CertificateOptions>>().Should().HaveCount(2);
        IChangeToken changeToken = tokenSources.OfType<ConfigurationChangeTokenSource<CertificateOptions>>().Single().GetChangeToken();

        bool changeCalled = false;
        _ = changeToken.RegisterChangeCallback(_ => changeCalled = true, "state");
        configuration[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:CertificateFilePath"] = certificate2FilePath;
        configuration[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:PrivateKeyFilePath"] = privateKey2FilePath;
        configuration.Reload();
        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(secondX509);
        changeCalled.Should().BeTrue("file path information changed");

        _ = changeToken.RegisterChangeCallback(_ => changeCalled = true, "state");
        await File.WriteAllTextAsync(certificate2FilePath, instance1Certificate);
        await File.WriteAllTextAsync(privateKey2FilePath, instance1PrivateKey);
        await Task.Delay(2000);
        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(firstX509);
        changeCalled.Should().BeTrue("file contents changed");
    }
}

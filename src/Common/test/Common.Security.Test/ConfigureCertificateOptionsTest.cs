// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Options;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void ConfigureCertificateOptions_NoPath_NoCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { $"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath", string.Empty }
        }).Build();

        var options = new ConfigureCertificateOptions(configurationRoot);
        var opts = new CertificateOptions();

        options.Configure(CertificateName, opts);

        opts.Certificate.Should().BeNull();
        CertificateName.Should().Be(opts.Name);
    }

    [Fact]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(CertificateName, "instance.p12").Build();
        configurationRoot[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:certificateFilePath"].Should().NotBeNull();
        var options = new ConfigureCertificateOptions(configurationRoot);
        var opts = new CertificateOptions();

        options.Configure(CertificateName, opts);

        opts.Certificate.Should().NotBeNull();
        CertificateName.Should().Be(opts.Name);
        opts.Certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificate()
    {
        const string certificateName = "test";
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificate(certificateName, "instance.crt", "instance.key").Build();
        var pemConfig = new ConfigureCertificateOptions(configurationRoot);

        var opts = new CertificateOptions
        {
            Name = certificateName
        };

        pemConfig.Configure(certificateName, opts);

        opts.Certificate.Should().NotBeNull();
        certificateName.Should().BeEquivalentTo(opts.Name);
        opts.Certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public async Task CertificateOptionsUpdateOnFileChange()
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

        IConfigurationRoot configuration = new ConfigurationBuilder().AddCertificate(CertificateName, certificateFilePath, privateKeyFilePath).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton<IConfiguration>(configuration).ConfigureCertificateOptions(configuration, null)
            .BuildServiceProvider();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();

        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(firstX509);

        await File.WriteAllTextAsync(certificateFilePath, secondCertificateContent);
        await File.WriteAllTextAsync(privateKeyFilePath, secondPrivateKeyContent);
        await Task.Delay(2000);

        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(secondX509);
    }

    [Fact]
    public async Task CertificateOptionsNotifyOnChange()
    {
        using var sandbox = new Sandbox();
        string instanceCertificate = await File.ReadAllTextAsync("instance.crt");
        string instancePrivateKey = await File.ReadAllTextAsync("instance.key");
        var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string instance2Certificate = await File.ReadAllTextAsync("instance2.crt");
        string instance2PrivateKey = await File.ReadAllTextAsync("instance2.key");
        var secondX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string certificateFilePath = sandbox.CreateFile("cert", instanceCertificate);
        string privateKeyFilePath = sandbox.CreateFile("key", instancePrivateKey);
        string certificate2FilePath = sandbox.CreateFile("cert2", instance2Certificate);
        string privateKey2FilePath = sandbox.CreateFile("key2", instance2PrivateKey);

        IConfigurationRoot configuration = new ConfigurationBuilder().AddCertificate(CertificateName, certificateFilePath, privateKeyFilePath).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton<IConfiguration>(configuration).ConfigureCertificateOptions(configuration, null)
            .BuildServiceProvider();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(firstX509);

        IEnumerable<IOptionsChangeTokenSource<CertificateOptions>> tokenSources = serviceProvider.GetServices<IOptionsChangeTokenSource<CertificateOptions>>();
        bool changeCalled = false;
        IChangeToken changeToken = tokenSources.First().GetChangeToken();
        _ = changeToken.RegisterChangeCallback(_ => changeCalled = true, "state");

        configuration[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:CertificateFilePath"] = certificate2FilePath;
        configuration[$"{CertificateOptions.ConfigurationKeyPrefix}:{CertificateName}:PrivateKeyFilePath"] = privateKey2FilePath;
        configuration.Reload();
        await Task.Delay(2000);
        optionsMonitor.Get(CertificateName).Certificate.Should().BeEquivalentTo(secondX509);
        changeCalled.Should().BeTrue("file path information changed");
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task ServiceLoadsCertificate()
    {
        using var personalCertificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        personalCertificateStore.Open(OpenFlags.ReadWrite);
        personalCertificateStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false).Should().BeEmpty();

        using var sandbox = new Sandbox();
        string filename = sandbox.CreateFile("fakeCertificate.p12");
        File.Copy("instance.p12", filename, true);

        IConfigurationRoot configuration = new ConfigurationBuilder().AddCertificate("ContainerIdentity", filename).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddSingleton<IConfigureNamedOptions<CertificateOptions>, ConfigureCertificateOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        X509Certificate2Collection collection =
            personalCertificateStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false);

        collection.Should().NotBeNull();

        if (!File.Exists(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
        {
            var orgId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var certificateWriter = new LocalCertificateWriter();

            certificateWriter.Write(orgId, spaceId);
        }

        X509Certificate2 certificate =
            GetX509FromCertKeyPair(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"),
                Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));

        await File.WriteAllBytesAsync(filename, certificate.Export(X509ContentType.Pkcs12));
        await Task.Delay(2000);

        X509Certificate2Collection newCollection =
            personalCertificateStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false);

        newCollection.Should().NotIntersectWith(collection);

        personalCertificateStore.Close();
    }

    private X509Certificate2 GetX509FromCertKeyPair(string certFile, string keyFile)
    {
        using var certificate = new X509Certificate2(certFile);
        using var key = RSA.Create();
        key.ImportFromPem(File.ReadAllText(keyFile));

        return certificate.CopyWithPrivateKey(key);
    }
}

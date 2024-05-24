// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public class CertificateRotationServiceTest
{
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

        IConfigurationRoot configuration = new ConfigurationBuilder().AddCertificateFile(ClientCertificates.ContainerIdentity, filename).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddSingleton<IConfigureNamedOptions<CertificateOptions>, ConfigureNamedCertificateOptions>();
        services.AddSingleton<CertificateRotationService>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var service = serviceProvider.GetRequiredService<CertificateRotationService>();

        service.Start();

        X509Certificate2Collection collection =
            personalCertificateStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false);

        collection.Should().NotBeNull();

        if (!File.Exists(Path.Combine(LocalCertificateWriter.ApplicationBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
        {
            var orgId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var certificateWriter = new LocalCertificateWriter();

            certificateWriter.Write(orgId, spaceId);
        }

        X509Certificate2 certificate =
            GetX509FromCertKeyPair(Path.Combine(LocalCertificateWriter.ApplicationBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"),
                Path.Combine(LocalCertificateWriter.ApplicationBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));

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

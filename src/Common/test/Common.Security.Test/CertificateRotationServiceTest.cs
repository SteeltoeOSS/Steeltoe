// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Utils.IO;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public class CertificateRotationServiceTest
{
    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task ServiceLoadsCertificate()
    {
        using var personalCertStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        personalCertStore.Open(OpenFlags.ReadWrite);
        personalCertStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false).Should().BeEmpty();

        using var sandbox = new Sandbox();
        var filename = sandbox.CreateFile("fakeCertificate.p12");
        File.Copy("instance.p12", filename, true);

        var config = new ConfigurationBuilder()
            .AddCertificateFile(filename)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddOptions();
        services.AddSingleton<IConfigureOptions<CertificateOptions>, ConfigureCertificateOptions>();
        services.AddSingleton<ICertificateRotationService, CertificateRotationService>();

        using var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ICertificateRotationService>();

        service.Start();

        var collection = personalCertStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false);
        collection.Should().NotBeNull();

#if NET6_0_OR_GREATER
        if (!File.Exists(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
        {
            var orgId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var certWriter = new LocalCertificateWriter();

            certWriter.Write(orgId, spaceId);
        }

        var cert = GetX509FromCertKeyPair(
            Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"),
            Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));
        await File.WriteAllBytesAsync(filename, cert.Export(X509ContentType.Pkcs12));
        await Task.Delay(2000);

        var newCollection = personalCertStore.Certificates.Find(X509FindType.FindByIssuerName, "Diego Instance Identity Intermediate CA", false);
        newCollection.Should().NotIntersectWith(collection);
#else
        // avoid warning about missing await for .NET Core 3.1 target
        await Task.Yield();
#endif

        personalCertStore.Close();
    }

#if NET6_0_OR_GREATER
    private X509Certificate2 GetX509FromCertKeyPair(string certFile, string keyFile)
    {
        using var cert = new X509Certificate2(certFile);
        using var key = RSA.Create();
        key.ImportFromPem(File.ReadAllText(keyFile));

        return cert.CopyWithPrivateKey(key);
    }
#endif
}
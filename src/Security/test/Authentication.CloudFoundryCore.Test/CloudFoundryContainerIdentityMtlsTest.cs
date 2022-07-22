// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Security;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryContainerIdentityMtlsTest : IClassFixture<ClientCertificatesFixture>
{
    private readonly ClientCertificatesFixture fixture;

    public CloudFoundryContainerIdentityMtlsTest(ClientCertificatesFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CloudFoundryCertificateAuth_AcceptsSameSpace()
    {
        var host = await GetHostBuilder().StartAsync();

        var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundryCertificateAuth_AcceptsSameOrg()
    {
        var host = await GetHostBuilder().StartAsync();

        var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameOrganizationAuthorizationPolicy);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundryCertificateAuth_RejectsOrgMismatch()
    {
        var host = await GetHostBuilder().StartAsync();

        var response = await ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameOrganizationAuthorizationPolicy);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundryCertificateAuth_RejectsSpaceMismatch()
    {
        var host = await GetHostBuilder().StartAsync();

        var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryCertificateAuth_ForbiddenWithoutCert()
    {
        var host = await GetHostBuilder().StartAsync();

        var response = await host.GetTestClient().GetAsync("http://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private IHostBuilder GetHostBuilder()
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity(fixture.ServerOrgId.ToString(), fixture.ServerSpaceId.ToString()))
            .ConfigureWebHostDefaults(webHost => webHost.UseStartup<TestServerCertificateStartup>())
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
            });
    }

    private HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate2 certificate)
    {
        var bytes = certificate.GetRawCertData();
        var b64 = Convert.ToBase64String(bytes);
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-Client-Cert", b64);
        return httpClient;
    }

    private static class Certificates
    {
        public static X509Certificate2 OrgAndSpaceMatch { get; } =
            new X509Certificate2(GetFullyQualifiedFilePath("OrgAndSpaceMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("OrgAndSpaceMatchKey.pem"))));

        public static X509Certificate2 OrgMatch { get; } =
            new X509Certificate2(GetFullyQualifiedFilePath("OrgMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("OrgMatchKey.pem"))));

        public static X509Certificate2 SpaceMatch { get; } =
            new X509Certificate2(GetFullyQualifiedFilePath("SpaceMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("SpaceMatchKey.pem"))));

        private static string GetFullyQualifiedFilePath(string filename)
        {
            var filePath = Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", filename);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            return filePath;
        }
    }
}
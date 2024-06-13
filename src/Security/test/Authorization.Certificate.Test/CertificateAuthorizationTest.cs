// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Certificate;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class CertificateAuthorizationTest(ClientCertificatesFixture fixture) : IClassFixture<ClientCertificatesFixture>
{
    [Fact]
    public async Task CertificateAuth_AcceptsSameSpace()
    {
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameSpaceAuthorizationPolicy}");
        HttpResponseMessage response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch).GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameOrg()
    {
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameOrganizationAuthorizationPolicy}");
        HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_RejectsOrgMismatch()
    {
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameOrganizationAuthorizationPolicy}");
        HttpResponseMessage response = await ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch).GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_RejectsSpaceMismatch()
    {
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameSpaceAuthorizationPolicy}");
        HttpResponseMessage response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch).GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_ForbiddenWithoutCert()
    {
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"http://localhost/{CertificateAuthorizationDefaults.SameSpaceAuthorizationPolicy}");
        HttpResponseMessage response = await host.GetTestClient().GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameSpace_DiegoCert()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameSpaceAuthorizationPolicy}");
        HttpResponseMessage response = await ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego).GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameOrg_DiegoCert()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));

        using IHost host = await GetHostBuilder().StartAsync();

        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationDefaults.SameOrganizationAuthorizationPolicy}");
        HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private IHostBuilder GetHostBuilder()
    {
    private IHostBuilder GetHostBuilder()
    {
        var hostBuilder = new HostBuilder();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate(fixture.ServerOrgId, fixture.ServerSpaceId));
        hostBuilder.ConfigureWebHostDefaults(builder => builder.UseStartup<TestServerCertificateStartup>());
        hostBuilder.ConfigureWebHost(builder => builder.UseTestServer());
        return hostBuilder;
    }
    }

    private static HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate certificate)
    {
        byte[] bytes = certificate.GetRawCertData();
        string b64 = Convert.ToBase64String(bytes);
        httpClient.DefaultRequestHeaders.Add("X-Client-Cert", b64);
        return httpClient;
    }

    private static class Certificates
    {
        private static readonly Func<string, string> GetFilePath = fileName =>
            Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", fileName);

        public static X509Certificate2 OrgAndSpaceMatch { get; } =
            X509Certificate2.CreateFromPemFile(GetFilePath("OrgAndSpaceMatchCert.pem"), GetFilePath("OrgAndSpaceMatchKey.pem"));

        public static X509Certificate2 OrgMatch { get; } = X509Certificate2.CreateFromPemFile(GetFilePath("OrgMatchCert.pem"), GetFilePath("OrgMatchKey.pem"));

        public static X509Certificate2 SpaceMatch { get; } =
            X509Certificate2.CreateFromPemFile(GetFilePath("SpaceMatchCert.pem"), GetFilePath("SpaceMatchKey.pem"));

        public static X509Certificate2 FromDiego { get; } = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
    }
}

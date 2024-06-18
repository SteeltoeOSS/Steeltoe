// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class CertificateAuthorizationTest
{
    [Fact]
    public async Task CertificateAuth_ForbiddenWithoutCert()
    {
        var requestUri = new Uri($"http://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameOrg()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrganization}");
        using IHost host = await GetHostBuilder().StartAsync();
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameSpace()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync();
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_RejectsOrgMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrganization}");
        using IHost host = await GetHostBuilder().StartAsync();
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_RejectsSpaceMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync();
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameSpace_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetHostBuilder().StartAsync();
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameOrg_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrganization}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetHostBuilder().StartAsync();
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_SetDefaultPolicyWithRequirements()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies().AddDefaultPolicy("sameOrgAndSpace",
            policyBuilder => policyBuilder.AddRequirements([
                new SameOrgRequirement(),
                new SameSpaceRequirement()
            ]));

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync();
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CertificateAuth_SetDefaultPolicyWithPolicyBuilder()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies()
            .AddDefaultPolicy("sameOrgAndSpace", policyBuilder => policyBuilder.RequireSameOrg().RequireSameSpace());

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync();
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HostBuilder GetHostBuilder()
    {
        var hostBuilder = new HostBuilder();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId));
        hostBuilder.ConfigureWebHostDefaults(builder => builder.UseStartup<TestServerCertificateStartup>());
        hostBuilder.ConfigureWebHost(builder => builder.UseTestServer());
        return hostBuilder;
    }

    private static HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate certificate)
    {
        byte[] bytes = certificate.GetRawCertData();
        string b64 = Convert.ToBase64String(bytes);
        httpClient.DefaultRequestHeaders.Add("X-Client-Cert", b64);
        return httpClient;
    }
}

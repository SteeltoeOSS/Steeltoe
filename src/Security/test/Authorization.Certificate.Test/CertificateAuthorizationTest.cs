// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class CertificateAuthorizationTest
{
    [Fact]
    public async Task CertificateAuth_ForbiddenWithoutCert()
    {
        var requestUri = new Uri($"http://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameOrg()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_AcceptsSameSpace()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_RejectsOrgMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CertificateAuth_RejectsSpaceMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task CertificateAuth_AcceptsSameSpace_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task CertificateAuth_AcceptsSameOrg_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_SetDefaultPolicyWithRequirements()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies().AddDefaultPolicy("sameOrgAndSpace",
            policyBuilder => policyBuilder.AddRequirements(new SameOrgRequirement(), new SameSpaceRequirement()));

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_SetDefaultPolicyWithPolicyBuilder()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies()
            .AddDefaultPolicy("sameOrgAndSpace", policyBuilder => policyBuilder.RequireSameOrg().RequireSameSpace());

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_AllowsCustomHeader()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies("a-custom-header")
            .AddDefaultPolicy("sameOrgAndSpace", policyBuilder => policyBuilder.RequireSameOrg().RequireSameSpace());

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate, "a-custom-header");

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HostBuilder GetHostBuilder()
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId));
        hostBuilder.ConfigureWebHost(builder => builder.UseStartup<TestServerCertificateStartup>());
        return hostBuilder;
    }

    private static HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate certificate, string certificateHeaderName = "X-Client-Cert")
    {
        byte[] bytes = certificate.GetRawCertData();
        string b64 = Convert.ToBase64String(bytes);
        httpClient.DefaultRequestHeaders.Add(certificateHeaderName, b64);
        return httpClient;
    }
}

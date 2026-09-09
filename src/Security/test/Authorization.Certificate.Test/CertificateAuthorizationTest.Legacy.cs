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

public sealed partial class CertificateAuthorizationTest
{
    private const string XClientCertHeaderName = "X-Client-Cert";

    [Fact]
    public async Task CertificateAuth_Legacy_ForbiddenWithoutCert()
    {
        var requestUri = new Uri($"http://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CertificateAuth_Legacy_AcceptsSameOrg()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_Legacy_AcceptsSameSpace()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), certificate, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_Legacy_RejectsOrgMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CertificateAuth_Legacy_RejectsSpaceMismatch()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task CertificateAuth_Legacy_AcceptsSameSpace_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameSpace}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task CertificateAuth_Legacy_AcceptsSameOrg_DiegoCert()
    {
        var requestUri = new Uri($"https://localhost/{CertificateAuthorizationPolicies.SameOrg}");
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using var caScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", Path.Join(LocalCertificateWriter.AppBasePath, "root_certificates"));
        using IHost host = await GetLegacyHostBuilder().StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = ClientWithCertificate(host.GetTestClient(), Certificates.FromDiego, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_Legacy_SetDefaultPolicyWithRequirements()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

#pragma warning disable CS0618 // Type or member is obsolete
        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies().AddDefaultPolicy("sameOrgAndSpace",
#pragma warning restore CS0618 // Type or member is obsolete
            policyBuilder => policyBuilder.AddRequirements(new SameOrgRequirement(), new SameSpaceRequirement()));

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;
        using HttpClient httpClient = ClientWithCertificate(application.GetTestClient(), certificate, XClientCertHeaderName);

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CertificateAuth_ObsoleteCustomHeaderOverload_UsesProvidedHeader()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

#pragma warning disable CS0618 // Type or member is obsolete
        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies(XClientCertHeaderName)
#pragma warning restore CS0618 // Type or member is obsolete
            .AddDefaultPolicy("sameOrgAndSpace", policyBuilder => policyBuilder.RequireSameOrg().RequireSameSpace());

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;

        using HttpClient configuredHeaderClient = ClientWithCertificate(application.GetTestClient(), certificate, XClientCertHeaderName);
        using HttpResponseMessage configuredHeaderResponse = await configuredHeaderClient.GetAsync(requestUri, TestContext.Current.CancellationToken);
        configuredHeaderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // X-Forwarded-Client-Cert is NOT accepted when a custom header was configured.
        using HttpClient defaultHeaderClient = ClientWithCertificate(application.GetTestClient(), certificate);
        using HttpResponseMessage defaultHeaderResponse = await defaultHeaderClient.GetAsync(requestUri, TestContext.Current.CancellationToken);
        defaultHeaderResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CertificateAuth_ObsoleteDefaultOverload_UsesLegacyDefaultHeader()
    {
        var requestUri = new Uri("https://localhost/request");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId);
        builder.Services.AddAuthentication().AddCertificate();

#pragma warning disable CS0618 // Type or member is obsolete
        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePolicies()
#pragma warning restore CS0618 // Type or member is obsolete
            .AddDefaultPolicy("sameOrgAndSpace", policyBuilder => policyBuilder.RequireSameOrg().RequireSameSpace());

        await using WebApplication application = builder.Build();
        application.UseCertificateAuthorization();
        application.MapGet("/request", () => "response").RequireAuthorization();
        await application.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = application.Services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        X509Certificate2 certificate = optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate!;

        using HttpClient legacyDefaultHeaderClient = ClientWithCertificate(application.GetTestClient(), certificate, XClientCertHeaderName);
        using HttpResponseMessage legacyDefaultHeaderResponse = await legacyDefaultHeaderClient.GetAsync(requestUri, TestContext.Current.CancellationToken);
        legacyDefaultHeaderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpClient forwardedHeaderClient = ClientWithCertificate(application.GetTestClient(), certificate);
        using HttpResponseMessage forwardedHeaderResponse = await forwardedHeaderClient.GetAsync(requestUri, TestContext.Current.CancellationToken);
        forwardedHeaderResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static HostBuilder GetLegacyHostBuilder(string? certificateHeaderName = null, Action<HostBuilder>? configureHost = null)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId));
        hostBuilder.ConfigureWebHost(builder => builder.UseStartup(_ => new LegacyTestServerCertificateStartup(certificateHeaderName)));
        configureHost?.Invoke(hostBuilder);
        return hostBuilder;
    }
}

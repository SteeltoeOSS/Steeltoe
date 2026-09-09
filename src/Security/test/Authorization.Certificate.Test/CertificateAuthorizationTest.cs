// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed partial class CertificateAuthorizationTest
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
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
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
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
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

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePoliciesForMutualTls().AddDefaultPolicy("sameOrgAndSpace",
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

        builder.Services.AddAuthorizationBuilder().AddOrgAndSpacePoliciesForMutualTls()
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
    public async Task PostConfigure_InvalidCertificateInSystemCertPath_LogsWarning()
    {
        using var sandbox = new Sandbox();
        string badCertPath = sandbox.CreateFile("bad-cert.crt", "this is not a valid PEM certificate");

        using var loggerProvider = new CapturingLoggerProvider((category, level) =>
            category == typeof(PostConfigureCertificateAuthenticationOptions).FullName && level == LogLevel.Warning);

        // ReSharper disable once AccessToDisposedClosure
        HostBuilder hostBuilder = GetHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider)));

        using IHost host = hostBuilder.Build();

        // AddAppInstanceIdentityCertificate sets CF_SYSTEM_CERT_PATH during Build(). Override it here so that
        // PostConfigure reads the sandbox path when IOptionsMonitor.Get() is called (lazy evaluation).
        using var certPathScope = new EnvironmentVariableScope("CF_SYSTEM_CERT_PATH", sandbox.FullPath);

        await host.StartAsync(TestContext.Current.CancellationToken);
        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CertificateAuthenticationOptions>>();

        // PostConfigure should not fire for unrelated scheme names.
        optionsMonitor.Get("some-other-scheme");
        loggerProvider.GetAll().Should().BeEmpty("PostConfigure should only run for the Certificate scheme");

        // PostConfigure should fire for the certificate auth scheme.
        optionsMonitor.Get(CertificateAuthenticationDefaults.AuthenticationScheme);

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().ContainSingle().Which.Should().Be(
            $"WARN {typeof(PostConfigureCertificateAuthenticationOptions)}: Failed to load system certificate from '{badCertPath}'. The file will be skipped.");
    }

    private static HostBuilder GetHostBuilder(Action<HostBuilder>? configureHost = null)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate(Certificates.ServerOrgId, Certificates.ServerSpaceId));
        hostBuilder.ConfigureWebHost(builder => builder.UseStartup<TestServerCertificateStartup>());
        configureHost?.Invoke(hostBuilder);
        return hostBuilder;
    }

    private static HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate certificate,
        string certificateHeaderName = "X-Forwarded-Client-Cert")
    {
        byte[] bytes = certificate.GetRawCertData();
        string b64 = Convert.ToBase64String(bytes);
        httpClient.DefaultRequestHeaders.Add(certificateHeaderName, b64);
        return httpClient;
    }
}

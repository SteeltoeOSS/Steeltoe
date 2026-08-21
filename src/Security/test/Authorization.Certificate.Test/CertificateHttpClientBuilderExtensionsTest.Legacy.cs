// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed partial class CertificateHttpClientBuilderExtensionsTest
{
    private const string XClientCertHeaderName = "X-Client-Cert";

    [Fact]
    public async Task AddAppInstanceIdentityCertificate_ObsoleteDefaultOverload_InjectsLegacyHeader()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        HostBuilder hostBuilder = GetHostBuilder(builder => builder.ConfigureServices(services =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddHttpClient("test").AddAppInstanceIdentityCertificate();
#pragma warning restore CS0618 // Type or member is obsolete
        }));

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var clientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient("test");
        client.DefaultRequestHeaders.Contains(XClientCertHeaderName).Should().BeTrue();

        using var instanceCertificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        byte[] certificateBytes = instanceCertificate.Export(X509ContentType.Cert);
        string certificateHeader = client.DefaultRequestHeaders.GetValues(XClientCertHeaderName).First();
        certificateHeader.Should().Be(Convert.ToBase64String(certificateBytes));
    }

    [Fact]
    public async Task AddClientCertificate_CertificateNotConfigured_LogsErrorAndSkipsHeaderInjection()
    {
        using var loggerProvider = new CapturingLoggerProvider((category, level) =>
            category == typeof(CertificateHttpClientBuilderExtensions).FullName && level == LogLevel.Error);

        // ReSharper disable once AccessToDisposedClosure
        HostBuilder hostBuilder = GetHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));

#pragma warning disable CS0618 // Type or member is obsolete
            builder.ConfigureServices(services => services.AddHttpClient("test").AddClientCertificate("does-not-exist"));
#pragma warning restore CS0618 // Type or member is obsolete
        });

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var clientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient("test");
        client.DefaultRequestHeaders.Should().BeEmpty();

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().Be(
            $"FAIL {typeof(CertificateHttpClientBuilderExtensions)}: Failed to find a certificate under the name 'does-not-exist' for HttpClient 'test'. It will not be attached to outbound requests in header 'X-Client-Cert'.");
    }

    [Fact]
    public async Task AddCertificateAuthorizationClient_ObsoleteCustomHeaderOverload_AllowsCustomHeader()
    {
        using var instanceCertificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        byte[] certificateBytes = instanceCertificate.Export(X509ContentType.Cert);
        const string customCertificateHeader = "my-arbitrary-header";
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        HostBuilder hostBuilder = GetHostBuilderWithCustomHeader(customCertificateHeader);
        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var clientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient("test");
        client.DefaultRequestHeaders.Should().Contain(header => header.Key == customCertificateHeader);
        client.DefaultRequestHeaders.GetValues(customCertificateHeader).First().Should().Be(Convert.ToBase64String(certificateBytes));
    }

    [Fact]
    public async Task AddAppInstanceIdentityCertificate_ObsoleteDefaultOverload_HeaderIsReceivedByServer()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        X509Certificate2? receivedCertificate = null;

        using X509Certificate2 serverCertificate = CreateTestServerCertificate();
        WebApplicationBuilder serverBuilder = WebApplication.CreateBuilder();

        serverBuilder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                // ReSharper disable once AccessToDisposedClosure
                listenOptions.UseHttps(serverCertificate);
            });
        });

        await using WebApplication server = serverBuilder.Build();

        server.MapGet("/", (HttpContext httpContext) =>
        {
            string? headerValue = httpContext.Request.Headers[XClientCertHeaderName];
            receivedCertificate = headerValue == null ? null : CreateCertificateFromRawData(Convert.FromBase64String(headerValue));
            return Results.Ok();
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        var addressesFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        int port = new Uri(addressesFeature.Addresses.First()).Port;

        HostBuilder clientHostBuilder = GetHostBuilder(builder => builder.ConfigureServices(services =>
        {
            IHttpClientBuilder httpClientBuilder = services.AddHttpClient("test");

            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // The test server uses a self-signed cert; skip server cert validation so only header-injection behavior is under test.
                SslOptions =
                {
#pragma warning disable CA5359 // Do Not Disable Certificate Validation
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
#pragma warning restore CA5359
                }
            });

#pragma warning disable CS0618 // Type or member is obsolete
            httpClientBuilder.AddAppInstanceIdentityCertificate();
#pragma warning restore CS0618 // Type or member is obsolete
        }));

        using IHost clientHost = await clientHostBuilder.StartAsync(TestContext.Current.CancellationToken);
        var clientFactory = clientHost.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient httpClient = clientFactory.CreateClient("test");
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"https://localhost:{port}/"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string instanceCertificatePem = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        var expectedCertificate = X509Certificate2.CreateFromPem(instanceCertificatePem);
        receivedCertificate.Should().NotBeNull();
        receivedCertificate.Thumbprint.Should().Be(expectedCertificate.Thumbprint);
    }

    private static X509Certificate2 CreateCertificateFromRawData(byte[] rawData)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(rawData);
#else
        return new X509Certificate2(rawData);
#endif
    }

    private static HostBuilder GetHostBuilderWithCustomHeader(string certificateHeaderName)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate());

        hostBuilder.ConfigureServices(services =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddHttpClient("test").AddAppInstanceIdentityCertificate(certificateHeaderName);
#pragma warning restore CS0618 // Type or member is obsolete
        });

        return hostBuilder;
    }
}

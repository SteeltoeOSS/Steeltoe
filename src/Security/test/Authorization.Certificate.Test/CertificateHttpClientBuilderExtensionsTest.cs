// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed partial class CertificateHttpClientBuilderExtensionsTest
{
    [Fact]
    public async Task AddAppInstanceIdentityCertificate_UsesMtlsWithoutHeaderInjection()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        HostBuilder hostBuilder = GetHostBuilder();
        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;
        socketsHandler.SslOptions.ClientCertificateContext.Should().NotBeNull();

        var clientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient("test");
        client.DefaultRequestHeaders.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAppInstanceIdentityCertificate_ClientCertificateIsReceivedByServer()
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
                listenOptions.UseHttps(serverCertificate, httpsOptions =>
                {
                    httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                    httpsOptions.ClientCertificateValidation = (_, _, _) => true;
                });
            });
        });

        await using WebApplication server = serverBuilder.Build();

        server.MapGet("/", (HttpContext httpContext) =>
        {
            receivedCertificate = httpContext.Connection.ClientCertificate;
            return Results.Ok();
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        var addressesFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        int port = new Uri(addressesFeature.Addresses.First()).Port;

        HostBuilder clientHostBuilder = GetHostBuilder();
        using IHost clientHost = await clientHostBuilder.StartAsync(TestContext.Current.CancellationToken);
        var handlerFactory = clientHost.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;

        // The test server uses a self-signed cert; skip server cert validation so only client cert behavior is under test.
#pragma warning disable CA5359 // Do Not Disable Certificate Validation
        socketsHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359

        // ReSharper disable once ShortLivedHttpClient
        using var httpClient = new HttpClient(handler, false);
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"https://localhost:{port}/"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string instanceCertificatePem = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        var expectedCertificate = X509Certificate2.CreateFromPem(instanceCertificatePem);
        receivedCertificate.Should().NotBeNull();
        receivedCertificate.Thumbprint.Should().Be(expectedCertificate.Thumbprint);
    }

    [Fact]
    public async Task AddClientCertificate_ExistingSocketsHttpHandler_IsReusedAndCertificateApplied()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        HostBuilder hostBuilder = GetHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var socketsHttpHandler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 99
            };

            IHttpClientBuilder httpClientBuilder = services.AddHttpClient("test");
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => socketsHttpHandler);
            httpClientBuilder.AddClientCertificateForMutualTls(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
        }));

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;

        socketsHandler.MaxConnectionsPerServer.Should().Be(99);
        socketsHandler.SslOptions.ClientCertificateContext.Should().NotBeNull();
    }

    [Fact]
    public async Task AddClientCertificate_IncompatiblePrimaryHandler_OnDotNet8_LogsReplacementAtDebugLevel()
    {
        if (Environment.Version.Major >= 9)
        {
            Assert.Skip("On .NET 9+, an explicitly configured incompatible primary handler throws instead of being replaced.");
        }

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        using var loggerProvider = new CapturingLoggerProvider((category, level) =>
            category == typeof(CertificateHttpClientBuilderExtensions).FullName && level == LogLevel.Debug);

        // ReSharper disable once AccessToDisposedClosure
        HostBuilder hostBuilder = GetHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug).AddProvider(loggerProvider));

            builder.ConfigureServices(services =>
                services.AddHttpClient("test").AddClientCertificateForMutualTls(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName));
        });

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;
        socketsHandler.SslOptions.ClientCertificateContext.Should().NotBeNull();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().ContainSingle().Which.Should().Be(
            $"DBUG {typeof(CertificateHttpClientBuilderExtensions)}: Replacing the PrimaryHandler on HttpClient 'test' with a new SocketsHttpHandler.");
    }

    [Fact]
    public async Task AddClientCertificateForMutualTls_CertificateNotConfigured_LogsErrorAndSkipsClientCertificate()
    {
        using var loggerProvider = new CapturingLoggerProvider((category, level) =>
            category == typeof(CertificateHttpClientBuilderExtensions).FullName && level == LogLevel.Error);

        // ReSharper disable once AccessToDisposedClosure
        HostBuilder hostBuilder = GetHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
            builder.ConfigureServices(services => services.AddHttpClient("test").AddClientCertificateForMutualTls("does-not-exist"));
        });

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;
        socketsHandler.SslOptions.ClientCertificateContext.Should().BeNull();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
            $"FAIL {typeof(CertificateHttpClientBuilderExtensions)}: Failed to find a certificate under the name 'does-not-exist'. The mTLS client certificate will not be available until this is resolved.",
            $"FAIL {typeof(CertificateHttpClientBuilderExtensions)}: HttpClient 'test' is being configured without an mTLS client certificate because certificate 'does-not-exist' is currently unavailable.");
    }

    [Fact]
    public async Task AddClientCertificate_IncompatiblePrimaryHandler_OnDotNet9Plus_ThrowsInvalidOperationException()
    {
        if (Environment.Version.Major < 9)
        {
            Assert.Skip("On .NET 8, an incompatible primary handler is replaced with a Debug log rather than throwing.");
        }

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        HostBuilder hostBuilder = GetHostBuilder(builder => builder.ConfigureServices(services =>
        {
            // User explicitly sets a non-SocketsHttpHandler; mTLS cannot be applied.
            IHttpClientBuilder httpClientBuilder = services.AddHttpClient("test");
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
            httpClientBuilder.AddClientCertificateForMutualTls(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
        }));

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        Action action = () => handlerFactory.CreateHandler("test");

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("HttpClient 'test' has an incompatible primary handler 'HttpClientHandler'. *");
    }

    [Fact]
    public async Task AddClientCertificate_SocketsHttpHandlerRegisteredAfterAddClientCertificate_IsStillReused()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");

        HostBuilder hostBuilder = GetHostBuilder(builder =>
        {
            // Registration order is reversed: AddClientCertificate first, then ConfigurePrimaryHttpMessageHandler.
            // PostConfigure always runs after Configure, so the handler is still reused regardless of call order.
            builder.ConfigureServices(services =>
            {
                var socketsHttpHandler = new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = 77
                };

                IHttpClientBuilder httpClientBuilder = services.AddHttpClient("test");
                httpClientBuilder.AddClientCertificateForMutualTls(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => socketsHttpHandler);
            });
        });

        using IHost host = await hostBuilder.StartAsync(TestContext.Current.CancellationToken);

        var handlerFactory = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using HttpMessageHandler handler = handlerFactory.CreateHandler("test");
        SocketsHttpHandler socketsHandler = GetInnerHandler(handler).Should().BeOfType<SocketsHttpHandler>().Which;

        socketsHandler.MaxConnectionsPerServer.Should().Be(77);
        socketsHandler.SslOptions.ClientCertificateContext.Should().NotBeNull();
    }

    private static X509Certificate2 CreateTestServerCertificate()
    {
        using var serverKey = RSA.Create();
        var request = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());

        // CreateSelfSigned produces an ephemeral (in-memory) key. Windows Schannel rejects ephemeral keys for
        // TLS, so round-trip through PKCS#12 with UserKeySet to store the key in a location Schannel accepts.
        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddHours(1));
        byte[] pkcs12Bytes = ephemeral.Export(X509ContentType.Pkcs12);

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pkcs12Bytes, null, X509KeyStorageFlags.UserKeySet);
#else
        return new X509Certificate2(pkcs12Bytes, (string?)null, X509KeyStorageFlags.UserKeySet);
#endif
    }

    private static HttpMessageHandler GetInnerHandler(HttpMessageHandler handler)
    {
        HttpMessageHandler inner = handler;

        while (inner is DelegatingHandler { InnerHandler: not null } delegating)
        {
            inner = delegating.InnerHandler;
        }

        return inner;
    }

    private static HostBuilder GetHostBuilder(Action<HostBuilder>? configureHost = null)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate());

        if (configureHost is null)
        {
            hostBuilder.ConfigureServices(services => services.AddHttpClient("test").AddAppInstanceIdentityCertificateForMutualTls());
        }
        else
        {
            configureHost(hostBuilder);
        }

        return hostBuilder;
    }
}

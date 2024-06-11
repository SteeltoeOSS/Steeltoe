// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class CertificateServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddCertificateAuthorizationClient_AddsNamedHttpClientWithCertificate()
    {
        byte[] certificateBytes = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key").Export(X509ContentType.Cert);
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using IHost host = await GetHostBuilder().StartAsync();
        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient(CertificateAuthorizationDefaults.HttpClientName);

        client.Should().NotBeNull();
        client.DefaultRequestHeaders.Contains("X-Client-Cert").Should().BeTrue();
        string certificateHeader = client.DefaultRequestHeaders.GetValues("X-Client-Cert").First();
        certificateHeader.Should().BeEquivalentTo(Convert.ToBase64String(certificateBytes));
    }

    private static IHostBuilder GetHostBuilder()
    {
        return new HostBuilder().ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate())
            .ConfigureServices(services => services.AddCertificateAuthorizationClient()).ConfigureWebHost(webBuilder =>
            {
                webBuilder.Configure(_ =>
                {
                });

                webBuilder.UseTestServer();
            });
    }
}

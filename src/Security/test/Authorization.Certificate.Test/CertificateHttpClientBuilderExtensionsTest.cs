// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class CertificateHttpClientBuilderExtensionsTest
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
        using HttpClient client = factory.CreateClient("test");

        client.Should().NotBeNull();
        client.DefaultRequestHeaders.Contains("X-Client-Cert").Should().BeTrue();
        string certificateHeader = client.DefaultRequestHeaders.GetValues("X-Client-Cert").First();
        certificateHeader.Should().BeEquivalentTo(Convert.ToBase64String(certificateBytes));
    }

    [Fact]
    public async Task AddCertificateAuthorizationClient_AllowsCustomHeader()
    {
        const string customCertificateHeader = "my-arbitrary-header";
        byte[] certificateBytes = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key").Export(X509ContentType.Cert);
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");
        using var certScope = new EnvironmentVariableScope("CF_INSTANCE_CERT", "instance.crt");
        using var keyScope = new EnvironmentVariableScope("CF_INSTANCE_KEY", "instance.key");
        using IHost host = await GetHostBuilder(customCertificateHeader).StartAsync();
        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("test");

        client.Should().NotBeNull();
        client.DefaultRequestHeaders.Contains(customCertificateHeader).Should().BeTrue();
        string certificateHeader = client.DefaultRequestHeaders.GetValues(customCertificateHeader).First();
        certificateHeader.Should().Be(Convert.ToBase64String(certificateBytes));
    }

    private static HostBuilder GetHostBuilder(string? certificateHeaderName = null)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddAppInstanceIdentityCertificate());

        hostBuilder.ConfigureServices(services =>
        {
            if (certificateHeaderName == null)
            {
                services.AddHttpClient("test").AddAppInstanceIdentityCertificate();
            }
            else
            {
                services.AddHttpClient("test").AddAppInstanceIdentityCertificate(certificateHeaderName);
            }
        });

        return hostBuilder;
    }
}

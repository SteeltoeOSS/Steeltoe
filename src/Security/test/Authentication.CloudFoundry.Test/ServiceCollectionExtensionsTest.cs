// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddCloudFoundryCertificateAuth_AddsServices()
    {
        var services = new ServiceCollection();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            {
                $"{CertificateOptions.ConfigurationKeyPrefix}:AppInstanceIdentity:CertificateFilePath",
                $"GeneratedCertificates{Path.DirectorySeparatorChar}SteeltoeInstanceCert.pem"
            }
        }).Build();

        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();

        services.AddCloudFoundryCertificateAuth(CertificateAuthenticationDefaults.AuthenticationScheme, null,
            new PhysicalFileProvider(LocalCertificateWriter.AppBasePath));

        ServiceProvider provider = services.BuildServiceProvider(true);

        provider.GetRequiredService<IOptions<CertificateOptions>>().Should().NotBeNull();
        provider.GetRequiredService<IConfigureOptions<CertificateOptions>>().Should().NotBeNull();
        provider.GetRequiredService<IOptionsChangeTokenSource<CertificateOptions>>().Should().NotBeNull();
        provider.GetRequiredService<IAuthorizationHandler>().Should().NotBeNull();
        var mtlsOptions = provider.GetRequiredService<IOptions<MutualTlsAuthenticationOptions>>();
        mtlsOptions.Should().NotBeNull();

        // confirm Events was set (in MutualTlsAuthenticationOptionsPostConfigurer.cs) vs being null by default
        mtlsOptions.Value.Events.Should().NotBeNull();
        new MutualTlsAuthenticationOptions().Events.Should().BeNull();
    }
}

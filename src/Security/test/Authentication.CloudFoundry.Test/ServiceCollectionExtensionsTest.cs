// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Security;
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
                $"{CertificateOptions.ConfigurationKeyPrefix}:ContainerIdentity:CertificateFilePath",
                $"GeneratedCertificates{Path.DirectorySeparatorChar}SteeltoeInstanceCert.pem"
            }
        }).Build();

        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();

        services.AddCloudFoundryCertificateAuth(configurationRoot, CertificateAuthenticationDefaults.AuthenticationScheme, null,
            new PhysicalFileProvider(LocalCertificateWriter.AppBasePath));

        ServiceProvider provider = services.BuildServiceProvider(true);

        Assert.NotNull(provider.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(provider.GetRequiredService<IConfigureOptions<CertificateOptions>>());
        Assert.NotNull(provider.GetRequiredService<IOptionsChangeTokenSource<CertificateOptions>>());
        Assert.NotNull(provider.GetRequiredService<IAuthorizationHandler>());
        var mtlsOpts = provider.GetRequiredService<IOptions<CertificateAuthenticationOptions>>();
        Assert.NotNull(mtlsOpts);

        // confirm Events was set (in MutualTlsAuthenticationOptionsPostConfigurer.cs) vs being null by default
        Assert.NotNull(mtlsOpts.Value.Events);
        Assert.Null(new CertificateAuthenticationOptions().Events);
    }
}

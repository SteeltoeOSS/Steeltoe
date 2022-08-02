// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddCloudFoundryCertificateAuth_ChecksNulls()
    {
        var servicesException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(null));
        Assert.Equal("services", servicesException.ParamName);
    }

    [Fact]
    public void AddCloudFoundryCertificateAuth_AddsServices()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();

        services.AddCloudFoundryCertificateAuth();
        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(provider.GetRequiredService<ICertificateRotationService>());
        Assert.NotNull(provider.GetRequiredService<IAuthorizationHandler>());
        var mtlsOpts = provider.GetRequiredService<IOptions<MutualTlsAuthenticationOptions>>();
        Assert.NotNull(mtlsOpts);

        // confirm Events was set (in MutualTlsAuthenticationOptionsPostConfigurer.cs) vs being null by default
        Assert.NotNull(mtlsOpts.Value.Events);
        Assert.Null(new MutualTlsAuthenticationOptions().Events);
    }
}

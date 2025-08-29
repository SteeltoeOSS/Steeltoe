// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using System;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddCloudFoundryCertificateAuth_ChecksNulls()
    {
        var sColl = new ServiceCollection();

        var servicesException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(null));
        Assert.Equal("services", servicesException.ParamName);
    }

    [Fact]
    public void AddCloudFoundryCertificateAuth_AddsServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();

        services.AddCloudFoundryCertificateAuth();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(provider.GetRequiredService<ICertificateRotationService>());
        Assert.NotNull(provider.GetRequiredService<IAuthorizationHandler>());
        Assert.NotNull(provider.GetRequiredService<IOptions<MutualTlsAuthenticationOptions>>());
    }

    [Fact]
    public void AddCloudFoundryCertificateAuth_SetsExpectedOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();

        services.AddCloudFoundryCertificateAuth();
        var provider = services.BuildServiceProvider();

        var mutualTlsOptions = provider.GetRequiredService<IOptionsSnapshot<MutualTlsAuthenticationOptions>>();
        var namedTlsOptions = mutualTlsOptions.Get(CertificateAuthenticationDefaults.AuthenticationScheme);

        // confirm Events was set (in MutualTlsAuthenticationOptionsPostConfigurer.cs) vs being null by default
        var defaultMTlsOptions = new MutualTlsAuthenticationOptions();
        Assert.NotNull(namedTlsOptions.Events);
        Assert.Null(defaultMTlsOptions.Events);
        Assert.Equal(defaultMTlsOptions.RevocationMode, namedTlsOptions.RevocationMode);

        var certificateForwardingOptions = provider.GetRequiredService<IOptions<CertificateForwardingOptions>>();
        Assert.Equal("X-Forwarded-Client-Cert", certificateForwardingOptions.Value.CertificateHeader);
    }

    [Fact]
    public void AddCloudFoundryCertificateAuth_AllowsOptionsCustomization()
    {
        var defaultMTlsOptions = new MutualTlsAuthenticationOptions();
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();

        static void TlsOptionsConfigurer(MutualTlsAuthenticationOptions options) => options.RevocationMode = X509RevocationMode.NoCheck;
        static void CertificateForwardingConfigurer(CertificateForwardingOptions options) => options.CertificateHeader = "some-custom-header";

        services.AddCloudFoundryCertificateAuth(CertificateAuthenticationDefaults.AuthenticationScheme, TlsOptionsConfigurer, CertificateForwardingConfigurer);
        var provider = services.BuildServiceProvider();

        var mutualTlsOptions = provider.GetRequiredService<IOptionsSnapshot<MutualTlsAuthenticationOptions>>();
        var namedTlsOptions = mutualTlsOptions.Get(CertificateAuthenticationDefaults.AuthenticationScheme);
        Assert.Equal(X509RevocationMode.NoCheck, namedTlsOptions.RevocationMode);
        Assert.NotEqual(defaultMTlsOptions.RevocationMode, namedTlsOptions.RevocationMode);

        var certForwardOptions = provider.GetRequiredService<IOptions<CertificateForwardingOptions>>();
        Assert.Equal("some-custom-header", certForwardOptions.Value.CertificateHeader);
    }
}
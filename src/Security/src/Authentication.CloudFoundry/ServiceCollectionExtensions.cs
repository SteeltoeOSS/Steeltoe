// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds options and services to use Cloud Foundry container identity certificates.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="fileProvider">
    /// Provides access to the file system.
    /// </param>
    public static void AddCloudFoundryContainerIdentity(this IServiceCollection services, IConfiguration configuration, IFileProvider fileProvider)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureCertificateOptions(configuration, "ContainerIdentity", fileProvider);
        services.AddSingleton<IPostConfigureOptions<CertificateAuthenticationOptions>, MutualTlsAuthenticationOptionsPostConfigurer>();
        services.AddSingleton<IAuthorizationHandler, CloudFoundryCertificateIdentityAuthorizationHandler>();
        services.AddCertificateForwarding(opt => opt.CertificateHeader = "X-Forwarded-Client-Cert");
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, IConfiguration configuration)
    {
        AddCloudFoundryCertificateAuth(services, configuration, CertificateAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the <see cref="MutualTlsAuthenticationOptions" />.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, IConfiguration configuration,
        Action<CertificateAuthenticationOptions> configurer)
    {
        AddCloudFoundryCertificateAuth(services, configuration, CertificateAuthenticationDefaults.AuthenticationScheme, configurer, null);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CertificateAuthenticationDefaults.AuthenticationScheme" />.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, IConfiguration configuration, string authenticationScheme)
    {
        AddCloudFoundryCertificateAuth(services, configuration, authenticationScheme, null, null);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CertificateAuthenticationDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the <see cref="MutualTlsAuthenticationOptions" />.
    /// </param>
    /// <param name="fileProvider">
    /// Provides access to the file system.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, IConfiguration configuration, string authenticationScheme,
        Action<CertificateAuthenticationOptions> configurer, IFileProvider fileProvider)
    {
        ArgumentGuard.NotNull(services);

        services.AddCloudFoundryContainerIdentity(configuration, fileProvider);

        services.AddAuthentication(authenticationScheme).AddCloudFoundryIdentityCertificate(authenticationScheme, configurer);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(CloudFoundryDefaults.SameOrganizationAuthorizationPolicy, builder => builder.SameOrg());
            options.AddPolicy(CloudFoundryDefaults.SameSpaceAuthorizationPolicy, builder => builder.SameSpace());
        });
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
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
    /// <param name="fileProvider">
    /// Provides access to the file system.
    /// </param>
    public static void AddCloudFoundryContainerIdentity(this IServiceCollection services, IFileProvider fileProvider)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureCertificateOptions("AppInstanceIdentity", fileProvider);
        services.AddSingleton<IPostConfigureOptions<MutualTlsAuthenticationOptions>, MutualTlsAuthenticationOptionsPostConfigurer>();
        services.AddSingleton<IAuthorizationHandler, CloudFoundryCertificateIdentityAuthorizationHandler>();
        services.AddCertificateForwarding(opt => opt.CertificateHeader = "X-Forwarded-Client-Cert");
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services)
    {
        AddCloudFoundryCertificateAuth(services, CertificateAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the <see cref="MutualTlsAuthenticationOptions" />.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, Action<MutualTlsAuthenticationOptions> configurer)
    {
        AddCloudFoundryCertificateAuth(services, CertificateAuthenticationDefaults.AuthenticationScheme, configurer, null);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CertificateAuthenticationDefaults.AuthenticationScheme" />.
    /// </param>
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, string authenticationScheme)
    {
        AddCloudFoundryCertificateAuth(services, authenticationScheme, null, null);
    }

    /// <summary>
    /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization.
    /// </summary>
    /// <param name="services">
    /// Service collection.
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
    public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, string authenticationScheme,
        Action<MutualTlsAuthenticationOptions> configurer, IFileProvider fileProvider)
    {
        ArgumentGuard.NotNull(services);

        services.AddCloudFoundryContainerIdentity(fileProvider);

        services.AddAuthentication(authenticationScheme).AddCloudFoundryIdentityCertificate(authenticationScheme, configurer);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(CloudFoundryDefaults.SameOrganizationAuthorizationPolicy, builder => builder.SameOrg());
            options.AddPolicy(CloudFoundryDefaults.SameSpaceAuthorizationPolicy, builder => builder.SameSpace());
        });
    }
}

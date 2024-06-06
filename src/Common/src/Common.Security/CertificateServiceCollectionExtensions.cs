// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public static class CertificateServiceCollectionExtensions
{
    /// <summary>
    /// Configure <see cref="CertificateOptions" /> for use with client certificates.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="certificateName">
    /// Name of the certificate used in configuration and IOptions.
    /// </param>
    /// <param name="fileProvider">
    /// Provides access to the file system.
    /// </param>
    public static IServiceCollection ConfigureCertificateOptions(this IServiceCollection services, IConfiguration configuration, string certificateName, IFileProvider? fileProvider)
    {
        fileProvider ??= new PhysicalFileProvider(Environment.CurrentDirectory);

        string configurationKey = string.IsNullOrEmpty(certificateName)
            ? CertificateOptions.ConfigurationKeyPrefix
            : ConfigurationPath.Combine(CertificateOptions.ConfigurationKeyPrefix, certificateName);

        services.Configure<CertificateOptions>(configuration.GetSection(configurationKey));
        services.WatchFilePathInOptions<CertificateOptions>(configuration, configurationKey, certificateName, "CertificateFileName", fileProvider);
        services.WatchFilePathInOptions<CertificateOptions>(configuration, configurationKey, certificateName, "PrivateKeyFileName", fileProvider);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CertificateOptions>, ConfigureCertificateOptions>());
        return services;
    }
}

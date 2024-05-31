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
    /// Configure <see cref="CertificateOptions"/> for use with client certificates.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The root <see cref="IConfiguration" /> to monitor for changes.
    /// </param>
    /// <param name="fileProvider">
    /// Provides access to the file system.
    /// </param>
    public static IServiceCollection ConfigureCertificateOptions(this IServiceCollection services, IConfiguration configuration, IFileProvider? fileProvider)
    {
        fileProvider ??= new PhysicalFileProvider(Environment.CurrentDirectory);
        IConfigurationSection[] childSections = configuration.GetSection(CertificateOptions.ConfigurationKeyPrefix).GetChildren().ToArray();

        foreach (IConfigurationSection childSection in childSections)
        {
            string configurationSectionKey = $"{CertificateOptions.ConfigurationKeyPrefix}{ConfigurationPath.KeyDelimiter}{childSection.Key}";
            services.Configure<CertificateOptions>(childSection);
            services.WatchFilePathInOptions<CertificateOptions>(configuration, configurationSectionKey, childSection.Key, "CertificateFileName", fileProvider);
            services.WatchFilePathInOptions<CertificateOptions>(configuration, configurationSectionKey, childSection.Key, "PrivateKeyFileName", fileProvider);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CertificateOptions>, ConfigureCertificateOptions>());
        return services;
    }
}

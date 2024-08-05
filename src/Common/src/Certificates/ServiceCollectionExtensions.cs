// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Common.Certificates;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Refreshes <see cref="IOptionsMonitor{TOptions}" /> when the file path in the specified property changes on disk, or the property value changes.
    /// </summary>
    /// <typeparam name="TOptions">
    /// The options type that contains <paramref name="pathPropertyName" />.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="key">
    /// The configuration key that <typeparamref name="TOptions" /> is bound to.
    /// </param>
    /// <param name="optionName">
    /// The named option, which gets added to <paramref name="key" />.
    /// </param>
    /// <param name="pathPropertyName">
    /// The name of the property that contains a file path.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection WatchFilePathInOptions<TOptions>(this IServiceCollection services, string key, string? optionName, string pathPropertyName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(pathPropertyName);

        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            string filePath = GetFilePath(configuration, key, optionName, pathPropertyName);
            var watcher = new FilePathInOptionsChangeTokenSource<TOptions>(optionName, filePath);

            _ = ChangeToken.OnChange(configuration.GetReloadToken, () =>
            {
                string newFilePath = GetFilePath(configuration, key, optionName, pathPropertyName);
                watcher.ChangePath(newFilePath);
            });

            return watcher;
        });

        return services;
    }

    private static string GetFilePath(IConfiguration configuration, string key, string? optionName, string propertyName)
    {
        string? filePath = configuration.GetValue<string>(string.IsNullOrEmpty(optionName)
            ? $"{key}{ConfigurationPath.KeyDelimiter}{propertyName}"
            : $"{key}{ConfigurationPath.KeyDelimiter}{optionName}{ConfigurationPath.KeyDelimiter}{propertyName}");

        return filePath ?? string.Empty;
    }
}

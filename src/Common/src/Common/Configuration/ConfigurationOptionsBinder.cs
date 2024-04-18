// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Configuration;

internal static class ConfigurationOptionsBinder
{
    /// <summary>
    /// Binds <typeparamref name="TOptions" /> against a configuration key and registers for change detection, so that
    /// <see cref="IOptionsMonitor{TOptions}" /> always returns the latest value. This method exists so that callers don't need to pass
    /// <see cref="IConfiguration" /> to set this up.
    /// </summary>
    /// <typeparam name="TOptions">
    /// The type of options to bind against configuration.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configurationKey">
    /// The configuration key to bind from.
    /// </param>
    /// <param name="configureAction">
    /// An optional action for additional configuration of <typeparamref name="TOptions" />.
    /// </param>
    public static void ConfigureReloadableOptions<TOptions>(this IServiceCollection services, string configurationKey,
        Action<TOptions, IServiceProvider>? configureAction = null)
        where TOptions : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configurationKey);

        OptionsBuilder<TOptions> builder = services.AddOptions<TOptions>();

        builder.Configure<IServiceProvider>((options, serviceProvider) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            IConfigurationSection section = configuration.GetSection(configurationKey);
            section.Bind(options);

            configureAction?.Invoke(options, serviceProvider);
        });

        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            IConfigurationSection section = configuration.GetSection(configurationKey);
            return new ConfigurationChangeTokenSource<TOptions>(section);
        });
    }
}

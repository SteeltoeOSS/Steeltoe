// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ApplicationInstanceInfo" /> for use with the options pattern. It is also registered as <see cref="IApplicationInstanceInfo" />
    /// in the IoC container for easy access.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static void AddApplicationInstanceInfo(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ApplicationInstanceInfo>, ConfigureApplicationInstanceInfo>());

        services.TryAddSingleton<IApplicationInstanceInfo>(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApplicationInstanceInfo>>();
            return optionsMonitor.CurrentValue;
        });
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

/// <summary>
/// Extension methods for adding services related to CloudFoundry.
/// </summary>
public static class CloudFoundryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CloudFoundryApplicationOptions" /> and <see cref="CloudFoundryServicesOptions" /> for use with the options pattern. The first
    /// type is also registered as <see cref="IApplicationInstanceInfo" /> in the IoC container for easy access.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddCloudFoundryOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<CloudFoundryServicesOptions>().BindConfiguration("vcap");

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ApplicationInstanceInfo>, ConfigureApplicationInstanceInfo>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CloudFoundryApplicationOptions>, ConfigureCloudFoundryApplicationOptions>());

        services.Replace(ServiceDescriptor.Singleton<IApplicationInstanceInfo>(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryApplicationOptions>>();
            return optionsMonitor.CurrentValue;
        }));

        return services;
    }

    /// <summary>
    /// Configures <see cref="ForwardedHeadersOptions" /> to use forwarded headers as they are provided in Cloud Foundry. Includes
    /// <see cref="ForwardedHeaders.XForwardedHost" /> and <see cref="ForwardedHeaders.XForwardedProto" />, and allows headers from proxy servers on any
    /// network unless KnownNetworks is configured in
    /// <c>
    /// Steeltoe:ForwardedHeaders:KnownNetworks
    /// </c>
    /// or on <see cref="ForwardedHeadersOptions" />, or
    /// <c>
    /// Steeltoe:ForwardedHeaders:TrustAllNetworks
    /// </c>
    /// is set to <c>false</c>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection" /> instance, for chaining.
    /// </returns>
    /// <remarks>
    /// IMPORTANT: <see cref="ForwardedHeadersExtensions.UseForwardedHeaders(IApplicationBuilder)" /> must be called separately to activate these options.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services" /> is <c>null</c>.
    /// </exception>
    public static IServiceCollection ConfigureForwardedHeadersOptionsForCloudFoundry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.AddOptions<ForwardedHeadersSettings>().BindConfiguration(ForwardedHeadersSettings.ConfigurationKey);
        services.AddOptions<ForwardedHeadersOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ForwardedHeadersOptions>, ConfigureForwardedHeadersOptions>());

        return services;
    }
}

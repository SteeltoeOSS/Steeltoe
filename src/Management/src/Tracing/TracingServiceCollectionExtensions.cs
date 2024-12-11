// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Extensions;
using Steeltoe.Logging;
using Steeltoe.Logging.DynamicLogger;

namespace Steeltoe.Management.Tracing;

public static class TracingServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IDynamicMessageProcessor" /> that adds tracing details from <see cref="Activity.Current" /> (if found) to log messages.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    /// This method also calls <see cref="LoggingBuilderExtensions.AddDynamicConsole" /> to ensure that an <see cref="IDynamicLoggerProvider" /> has been
    /// registered.
    /// </remarks>
    public static IServiceCollection AddTracingLogProcessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApplicationInstanceInfo();
        services.AddLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDynamicMessageProcessor, TracingLogProcessor>());

        return services;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;
using Steeltoe.Common.Extensions;
using Steeltoe.Logging;

namespace Steeltoe.Management.Tracing;

public static class TracingServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IDynamicMessageProcessor" /> that adds details of <see cref="Tracer.CurrentSpan" /> (if found) to log messages.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddTracingLogProcessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApplicationInstanceInfo();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDynamicMessageProcessor, TracingLogProcessor>());

        return services;
    }
}

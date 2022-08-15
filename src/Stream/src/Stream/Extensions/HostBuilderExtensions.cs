// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.StreamHost;

namespace Steeltoe.Stream.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddStreamServices<T>(this IHostBuilder builder)
    {
        return builder.AddSpringBootConfiguration().ConfigureServices((context, services) =>
        {
            services.AddStreamServices<T>(context.Configuration);
            services.AddHostedService<StreamLifeCycleService>();
        });
    }

    public static IWebHostBuilder AddStreamServices<T>(this IWebHostBuilder builder)
    {
        return builder.AddSpringBootConfiguration().ConfigureServices((context, services) =>
        {
            services.AddStreamServices<T>(context.Configuration);
            services.AddHostedService<StreamLifeCycleService>();
        });
    }

    public static WebApplicationBuilder AddStreamServices<T>(this WebApplicationBuilder builder)
    {
        builder.AddSpringBootConfiguration();
        builder.Services.AddStreamServices<T>(builder.Configuration);
        builder.Services.AddHostedService<StreamLifeCycleService>();
        return builder;
    }
}

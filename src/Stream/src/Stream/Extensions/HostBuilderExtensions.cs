// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Configuration.SpringBoot;
using Steeltoe.Stream.StreamHost;

namespace Steeltoe.Stream.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddStreamServices<T>(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddSpringBootFromEnvironmentVariable();
            configurationBuilder.AddSpringBootFromCommandLine(context.Configuration);
        }).ConfigureServices((context, services) =>
        {
            services.AddStreamServices<T>(context.Configuration);
            services.AddHostedService<StreamLifeCycleService>();
        });
    }

    public static IWebHostBuilder AddStreamServices<T>(this IWebHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddSpringBootFromEnvironmentVariable();
            configurationBuilder.AddSpringBootFromCommandLine(context.Configuration);
        }).ConfigureServices((context, services) =>
        {
            services.AddStreamServices<T>(context.Configuration);
            services.AddHostedService<StreamLifeCycleService>();
        });
    }

    public static WebApplicationBuilder AddStreamServices<T>(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddSpringBootFromEnvironmentVariable();
        builder.Configuration.AddSpringBootFromCommandLine(builder.Configuration);
        builder.Services.AddStreamServices<T>(builder.Configuration);
        builder.Services.AddHostedService<StreamLifeCycleService>();
        return builder;
    }
}

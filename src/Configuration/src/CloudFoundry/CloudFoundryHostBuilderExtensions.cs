// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration();

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration();

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The application builder.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration();

        return builder;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public static partial class ConfigServerHostBuilderExtensions
{
    /// <summary>
    /// Add Config Server and Cloud Foundry as application configuration sources.
    /// Also adds Config Server health check contributor and related services to the service container.
    /// </summary>
    /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    public static WebApplicationBuilder AddConfigServer(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory = null)
    {
        applicationBuilder.Configuration.AddConfigServer(applicationBuilder.Environment, loggerFactory);
        applicationBuilder.Services.AddConfigServerServices();
        return applicationBuilder;
    }
}
#endif

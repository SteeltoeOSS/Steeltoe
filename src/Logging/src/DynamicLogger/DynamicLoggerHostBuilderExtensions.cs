// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

namespace Steeltoe.Logging.DynamicLogger;

public static class DynamicLoggerHostBuilderExtensions
{
    /// <summary>
    /// Adds Dynamic Console Logging to your application. Removes ConsoleLoggerProvider if found (to prevent duplicate console log entries).
    /// <para />
    /// Also calls ILoggingBuilder.AddConfiguration() if not previously called.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddDynamicLogging(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureLogging((_, configureLogging) => configureLogging.AddDynamicConsole());
    }

    /// <summary>
    /// Adds Dynamic Console Logging to your application. Removes ConsoleLoggerProvider if found (to prevent duplicate console log entries).
    /// <para />
    /// Also calls ILoggingBuilder.AddConfiguration() if not previously called.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static WebApplicationBuilder AddDynamicLogging(this WebApplicationBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        hostBuilder.Logging.AddDynamicConsole();
        return hostBuilder;
    }
}

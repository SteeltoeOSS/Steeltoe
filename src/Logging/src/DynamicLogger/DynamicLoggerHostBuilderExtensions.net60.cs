// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Extensions.Logging;

public static partial class DynamicLoggerHostBuilderExtensions
{
    /// <summary>
    /// Adds Dynamic Console Logging to your application. Removes ConsoleLoggerProvider if found (to prevent duplicate console log entries).<para />
    /// Also calls ILoggingBuilder.AddConfiguration() if not previously called.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static WebApplicationBuilder AddDynamicLogging(this WebApplicationBuilder hostBuilder)
    {
        if (hostBuilder is null)
        {
            throw new ArgumentNullException(nameof(hostBuilder));
        }

        hostBuilder.Logging.AddDynamicConsole();
        return hostBuilder;
    }
}
#endif

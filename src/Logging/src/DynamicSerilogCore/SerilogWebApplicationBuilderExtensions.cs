// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Serilog;
using System;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    public static class SerilogWebApplicationBuilderExtensions
    {
        /// <summary>
        /// Configure Serilog as the <see cref="IDynamicLoggerProvider"/> to enable dynamically controlling log levels via management endpoints
        /// </summary>
        /// <param name="hostBuilder">The <see cref="WebApplicationBuilder"/> to configure</param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveDefaultConsole">When true, do not remove Microsoft's ConsoleLoggerProvider</param>
        /// <returns>The <see cref="WebApplicationBuilder"/></returns>
        public static WebApplicationBuilder AddDynamicSerilog(
            this WebApplicationBuilder hostBuilder,
            Action<WebApplicationBuilder, LoggerConfiguration> configureLogger = null,
            bool preserveDefaultConsole = false)
        {
            LoggerConfiguration loggerConfiguration = null;
            if (configureLogger is object)
            {
                loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(hostBuilder.Configuration);
                configureLogger(hostBuilder, loggerConfiguration);
            }

            hostBuilder.Logging.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
            return hostBuilder;
        }
    }
}
#endif
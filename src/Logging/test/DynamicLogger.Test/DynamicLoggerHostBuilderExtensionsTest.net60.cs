// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicLogger.Test;

public partial class DynamicLoggerHostBuilderExtensionsTest
{
    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_AddsDynamicLogging()
    {
        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.AddDynamicLogging();
        var host = hostBuilder.Build();
        var loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.Single(loggerProviders.Where(provider => provider is DynamicConsoleLoggerProvider));
    }

    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_RemovesConsoleLogging()
    {
        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Logging.AddConsole();
        hostBuilder.AddDynamicLogging();

        var host = hostBuilder.Build();
        var loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.DoesNotContain(loggerProviders, lp => lp is ConsoleLoggerProvider);
        Assert.Single(loggerProviders.Where(provider => provider is DynamicConsoleLoggerProvider));
    }
}
#endif

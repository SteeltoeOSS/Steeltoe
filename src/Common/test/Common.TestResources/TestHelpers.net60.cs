// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Steeltoe;

public static partial class TestHelpers
{
    public static ILoggerFactory GetLoggerFactory()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        serviceCollection.AddLogging(builder => builder.AddConsole());
        serviceCollection.AddLogging(builder => builder.AddDebug());
        return serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>();
    }

    public static WebApplicationBuilder GetTestWebApplicationBuilder(string[] args = null)
    {
        var webAppBuilder = WebApplication.CreateBuilder(args);
        webAppBuilder.Configuration.AddInMemoryCollection(FastTestsConfiguration);
        webAppBuilder.WebHost.UseTestServer();
        return webAppBuilder;
    }
}
#endif

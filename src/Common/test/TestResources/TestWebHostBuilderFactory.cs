// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable ASPDEPR004 // 'WebHostBuilder' is deprecated in favor of HostBuilder and WebApplicationBuilder.

namespace Steeltoe.Common.TestResources;

public static class TestWebHostBuilderFactory
{
    private static readonly Action<IApplicationBuilder> EmptyAction = _ =>
    {
    };

    private static readonly Action<ServiceProviderOptions> ConfigureServiceProvider = options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    };

    public static WebHostBuilder Create()
    {
        return Create(true);
    }

    public static WebHostBuilder Create(bool useTestServer)
    {
        var builder = new WebHostBuilder();
        ConfigureBuilder(builder, useTestServer, true);

        return builder;
    }

    private static void ConfigureBuilder(WebHostBuilder builder, bool useTestServer, bool deactivateDiagnostics)
    {
        builder.UseDefaultServiceProvider(ConfigureServiceProvider);
        builder.Configure(EmptyAction);

        if (useTestServer)
        {
            builder.UseTestServer();
        }

        if (deactivateDiagnostics)
        {
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<DiagnosticListener, InactiveDiagnosticListener>()));
        }
    }
}

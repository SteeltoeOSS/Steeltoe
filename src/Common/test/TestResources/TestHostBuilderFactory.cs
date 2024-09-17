// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.TestResources;

public static class TestHostBuilderFactory
{
    private static readonly Action<IApplicationBuilder> EmptyAction = _ =>
    {
    };

    private static readonly Action<ServiceProviderOptions> ConfigureServiceProvider = options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    };

    public static IHostBuilder Create()
    {
        var builder = new HostBuilder();
        ConfigureBuilder(builder, false, false);

        return builder;
    }

    public static IHostBuilder CreateWeb()
    {
        var builder = new HostBuilder();
        ConfigureBuilder(builder, true, true);

        return builder;
    }

    private static void ConfigureBuilder(IHostBuilder builder, bool configureWebHost, bool useTestServer)
    {
        builder.UseDefaultServiceProvider(ConfigureServiceProvider);

        if (configureWebHost)
        {
            builder.ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.Configure(EmptyAction);

                if (useTestServer)
                {
                    webHostBuilder.UseTestServer();
                }
            });
        }
    }
}

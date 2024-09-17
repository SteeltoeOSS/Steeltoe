// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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

    public static IWebHostBuilder Create()
    {
        return Create(true);
    }

    public static IWebHostBuilder Create(bool useTestServer)
    {
        var builder = new WebHostBuilder();
        ConfigureBuilder(builder, useTestServer);

        return builder;
    }

    private static void ConfigureBuilder(IWebHostBuilder builder, bool useTestServer)
    {
        builder.UseDefaultServiceProvider(ConfigureServiceProvider);
        builder.Configure(EmptyAction);

        if (useTestServer)
        {
            builder.UseTestServer();
        }
    }
}

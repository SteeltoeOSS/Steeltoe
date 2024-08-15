// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Steeltoe.Common.TestResources;

public static class TestWebApplicationBuilderFactory
{
    public static WebApplicationBuilder Create()
    {
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        ConfigureBuilder(builder, true);

        return builder;
    }

    public static WebApplicationBuilder Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            Args = args
        });

        ConfigureBuilder(builder, true);

        return builder;
    }

    // CAUTION: This method creates a full-blown host builder. Prefer to use an empty one instead, to verify all dependencies are registered.
    public static WebApplicationBuilder CreateDefault()
    {
        return CreateDefault(true);
    }

    // CAUTION: This method creates a full-blown host builder. Prefer to use an empty one instead, to verify all dependencies are registered.
    public static WebApplicationBuilder CreateDefault(bool useTestServer)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        ConfigureBuilder(builder, useTestServer);

        return builder;
    }

    private static void ConfigureBuilder(WebApplicationBuilder builder, bool useTestServer)
    {
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        if (useTestServer)
        {
            builder.WebHost.UseTestServer();
        }
    }
}

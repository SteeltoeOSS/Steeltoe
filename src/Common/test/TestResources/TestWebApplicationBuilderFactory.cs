// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Steeltoe.Common.TestResources;

public static class TestWebApplicationBuilderFactory
{
    /// <summary>
    /// Creates an empty builder with activated test server.
    /// </summary>
    public static WebApplicationBuilder Create()
    {
        return Create(new WebApplicationOptions());
    }

    /// <summary>
    /// Creates an empty builder with activated test server using the specified command-line arguments.
    /// </summary>
    public static WebApplicationBuilder Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = new WebApplicationOptions
        {
            Args = args
        };

        return Create(options);
    }

    /// <summary>
    /// Creates an empty builder with activated test server using the specified options.
    /// </summary>
    public static WebApplicationBuilder Create(WebApplicationOptions options)
    {
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(options);
        ConfigureBuilder(builder, true);

        return builder;
    }

    /// <summary>
    /// Creates a default builder with activated test server.
    /// <para>
    /// CAUTION: This method creates a full-blown host builder. Prefer to use an empty one instead, to verify all dependencies are registered.
    /// </para>
    /// </summary>
    public static WebApplicationBuilder CreateDefault()
    {
        return CreateDefault(true);
    }

    /// <summary>
    /// Creates a default builder, optionally with activated test server.
    /// <para>
    /// CAUTION: This method creates a full-blown host builder. Prefer to use an empty one instead, to verify all dependencies are registered.
    /// </para>
    /// </summary>
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

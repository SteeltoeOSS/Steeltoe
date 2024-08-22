// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Common.TestResources;

public static class TestWebHostBuilderFactory
{
    private static readonly Action<IApplicationBuilder> EmptyAction = _ =>
    {
    };

    public static IWebHostBuilder Create()
    {
        var builder = new WebHostBuilder();
        ConfigureBuilder(builder);

        return builder;
    }

    private static void ConfigureBuilder(IWebHostBuilder builder)
    {
        builder.Configure(EmptyAction);
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
    }
}

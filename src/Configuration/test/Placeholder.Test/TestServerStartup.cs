// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public sealed class TestServerStartup
{
    private readonly IConfiguration _configuration;

    public static IServiceProvider ServiceProvider { get; set; }

    public TestServerStartup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigurePlaceholderResolver(_configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        ServiceProvider = app.ApplicationServices;
    }
}

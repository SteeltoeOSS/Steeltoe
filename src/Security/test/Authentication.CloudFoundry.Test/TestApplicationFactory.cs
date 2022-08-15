// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class TestApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    private readonly IReadOnlyDictionary<string, string> _configuration;

    public TestApplicationFactory(IReadOnlyDictionary<string, string> configuration = null)
    {
        _configuration = configuration ?? ImmutableDictionary<string, string>.Empty;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        return base.CreateHost(builder);
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webHostBuilder =>
        {
            webHostBuilder.UseStartup<TStartup>().UseTestServer();
        }).ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(_configuration);
        });

        return builder;
    }
}

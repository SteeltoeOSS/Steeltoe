// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Configuration.Encryption.ConfigServer;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class StartupForConfigureConfigServerEncryptionResolver
{
    private readonly IConfiguration _configuration;
   
    internal static IServiceProvider ServiceProvider { get; private set; }

    public StartupForConfigureConfigServerEncryptionResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureConfigServerEncryptionResolver(_configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        ServiceProvider = app.ApplicationServices;
    }
}

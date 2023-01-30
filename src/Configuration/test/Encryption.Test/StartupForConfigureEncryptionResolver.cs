// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class StartupForConfigureEncryptionResolver
{
    private readonly IConfiguration _configuration;
    private readonly ITextDecryptor _textDecryptor;

    internal static IServiceProvider ServiceProvider { get; private set; }

    public StartupForConfigureEncryptionResolver(IConfiguration configuration)
    {
        _configuration = configuration;
        _textDecryptor = new TextDecryptor();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureEncryptionResolver(_configuration, _textDecryptor);
    }

    public void Configure(IApplicationBuilder app)
    {
        ServiceProvider = app.ApplicationServices;
    }
}

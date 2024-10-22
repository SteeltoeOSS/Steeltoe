// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.TestResources;

public static class TestHostApplicationBuilderFactory
{
    private static readonly ServiceProviderOptions ValidatingServiceProviderOptions = new()
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    };

    /// <summary>
    /// Creates an empty builder.
    /// </summary>
    public static HostApplicationBuilder Create()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        ConfigureBuilder(builder);

        return builder;
    }

    /// <summary>
    /// Creates an empty builder using the specified command-line arguments.
    /// </summary>
    /// <param name="args">
    /// The command-line arguments.
    /// </param>
    public static HostApplicationBuilder Create(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args
        });

        ConfigureBuilder(builder);

        return builder;
    }

    private static void ConfigureBuilder(HostApplicationBuilder builder)
    {
        builder.ConfigureContainer(new DefaultServiceProviderFactory(ValidatingServiceProviderOptions));
    }
}

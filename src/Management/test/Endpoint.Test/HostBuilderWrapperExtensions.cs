// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Endpoint.Test;

internal static class HostBuilderWrapperExtensions
{
    public static HostWrapper Build(this HostBuilderType hostBuilderType, Action<HostBuilderWrapper> configureHostBuilder)
    {
        return hostBuilderType switch
        {
            HostBuilderType.Host => CreateFromHostBuilder(configureHostBuilder),
            HostBuilderType.WebHost => CreateFromWebHostBuilder(configureHostBuilder),
            HostBuilderType.WebApplication => CreateFromWebApplicationBuilder(configureHostBuilder),
            HostBuilderType.HostApplication => CreateFromHostApplicationBuilder(configureHostBuilder),
            _ => throw new NotSupportedException()
        };
    }

    private static HostWrapper CreateFromHostBuilder(Action<HostBuilderWrapper> configureHostBuilder)
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        IHost host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }

    private static HostWrapper CreateFromWebHostBuilder(Action<HostBuilderWrapper> configureHostBuilder)
    {
        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        IWebHost host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }

    private static HostWrapper CreateFromWebApplicationBuilder(Action<HostBuilderWrapper> configureHostBuilder)
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        WebApplication host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }

    private static HostWrapper CreateFromHostApplicationBuilder(Action<HostBuilderWrapper> configureHostBuilder)
    {
        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        IHost host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }
}

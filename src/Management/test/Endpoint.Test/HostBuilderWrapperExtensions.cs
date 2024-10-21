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
            HostBuilderType.Host => CreateHost(configureHostBuilder),
            HostBuilderType.WebHost => CreateWebHost(configureHostBuilder),
            HostBuilderType.WebApplication => CreateWebApplication(configureHostBuilder),
            _ => throw new NotSupportedException()
        };
    }

    private static HostWrapper CreateHost(Action<HostBuilderWrapper> configureHostBuilder)
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        IHost host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }

    private static HostWrapper CreateWebHost(Action<HostBuilderWrapper> configureHostBuilder)
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        IWebHost host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }

    private static HostWrapper CreateWebApplication(Action<HostBuilderWrapper> configureHostBuilder)
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        HostBuilderWrapper hostBuilderWrapper = HostBuilderWrapper.Wrap(hostBuilder);
        configureHostBuilder(hostBuilderWrapper);

        WebApplication host = hostBuilder.Build();
        return HostWrapper.Wrap(host);
    }
}

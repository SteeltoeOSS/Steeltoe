// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ManagementPort;

/// <summary>
/// Adds the configured management port to the list of addresses that ASP.NET listens on.
/// </summary>
/// <remarks>
/// Because there's no way to set listen ports per endpoint, <see cref="ManagementPortMiddleware" /> blocks non-permitted requests.
/// </remarks>
internal sealed class ManagementPortStartupFilter : IStartupFilter
{
    private const string ManagementPortConfigurationKey = "Management:Endpoints:Port";
    private const string ManagementPortSslConfigurationKey = "Management:Endpoints:SslEnabled";
    private const int AspNetDefaultListenPort = 5000;

    private readonly IConfiguration _configuration;

    public ManagementPortStartupFilter(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentGuard.NotNull(next);

        return applicationBuilder =>
        {
            // Run as late as possible, so we'll observe the effects of other code that changed bindings.
            // Known limitation: this doesn't take bindings into account resulting from custom code that directly configures Kestrel.
            next(applicationBuilder);

            int managementPort = _configuration.GetValue<int?>(ManagementPortConfigurationKey) ?? 0;
            bool useHttps = _configuration.GetValue<bool?>(ManagementPortSslConfigurationKey) ?? false;

            if (managementPort > 0)
            {
                var server = applicationBuilder.ApplicationServices.GetRequiredService<IServer>();
                ICollection<string> addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;

                foreach (BindingAddress address in addresses.Select(BindingAddress.Parse))
                {
                    if (address.Port == managementPort && IsSameScheme(address.Scheme, useHttps))
                    {
                        // Scheme/port combination already exists. Duplicates are not allowed.
                        return;
                    }
                }

                if (addresses.Count == 0 && (managementPort != AspNetDefaultListenPort || useHttps))
                {
                    // Add the ultimate default binding explicitly, so our addition doesn't exclude it.
                    addresses.Add($"http://localhost:{AspNetDefaultListenPort}");
                }

                addresses.Add(useHttps ? $"https://*:{managementPort}" : $"http://*:{managementPort}");
            }
        };
    }

    private static bool IsSameScheme(string addressScheme, bool useHttps)
    {
        return useHttps ? addressScheme == Uri.UriSchemeHttps : addressScheme == Uri.UriSchemeHttp;
    }
}

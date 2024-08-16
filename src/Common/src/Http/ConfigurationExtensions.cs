// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Http;

internal static class ConfigurationExtensions
{
    // Found at https://github.com/dotnet/aspnetcore/blob/27f2a011a4211118552dfb8f38d36e8629267d2b/src/Hosting/Hosting/src/Internal/WebHost.cs#L24.
    private const string DeprecatedServerUrlsConfigurationKey = "server.urls";

    /// <summary>
    /// Best-effort attempt to discover the addresses this ASP.NET application binds to at startup.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    public static ICollection<string> GetListenAddresses(this IConfiguration configuration)
    {
        // Sources, based on https://andrewlock.net/8-ways-to-set-the-urls-for-an-aspnetcore-app:
        // - WebApplicationBuilder.WebHost.UseUrls() -> configuration["urls"]
        // - app.Urls.Add() just before app.Run() -> only detectable by IHostedLifecycleService.StartedAsync from IServer.Features.Get<IServerAddressesFeature>()
        // - Environment variable URLS, HTTP_PORTS, HTTPS_PORTS (with optional prefixes ASPNETCORE_ and DOTNET_) -> configuration["urls"], configuration["http_ports"], configuration["https_ports"]
        // - Command line arguments --urls, --http_ports, --https_ports -> configuration["urls"], configuration["http_ports"], configuration["https_ports"]
        // - applicationUrl in launchSettings.json -> configuration["urls"] (can be disabled with: dotnet run --no-launch-profile)
        // - KestrelServerOptions.Listen() -> configuration["Kestrel:Endpoints.*.Url"] (when setup from code, only detectable by IHostedLifecycleService)

        List<string> addresses = GetListenAddressesForKestrel(configuration).ToList();

        if (addresses.Count > 0)
        {
            return addresses;
        }

        string? urls = configuration["urls"] ?? configuration[DeprecatedServerUrlsConfigurationKey];

        if (string.IsNullOrEmpty(urls))
        {
            string httpPorts = configuration["http_ports"] ?? string.Empty;
            string httpsPorts = configuration["https_ports"] ?? string.Empty;

            string httpUrls = PortsToAddresses(httpPorts, Uri.UriSchemeHttp);
            string httpsUrls = PortsToAddresses(httpsPorts, Uri.UriSchemeHttps);

            urls = $"{httpUrls};{httpsUrls}";
        }

        foreach (string value in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            addresses.Add(value);
        }

        if (addresses.Count == 0)
        {
            addresses.Add("http://localhost:5000");
        }

        return addresses;
    }

    private static string PortsToAddresses(string ports, string scheme)
    {
        return string.Join(';',
            ports.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(port => $"{scheme}://*:{port}"));
    }

    private static IEnumerable<string> GetListenAddressesForKestrel(IConfiguration configuration)
    {
        IConfigurationSection endpointsSection = configuration.GetSection("Kestrel:Endpoints");

        foreach (IConfigurationSection endpointSection in endpointsSection.GetChildren())
        {
            string? url = endpointSection["Url"];

            if (url != null)
            {
                yield return url;
            }
        }
    }
}

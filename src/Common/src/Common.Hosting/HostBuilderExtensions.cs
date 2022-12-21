// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Common.Hosting;

public static class HostBuilderExtensions
{
    private const string DeprecatedServerUrlsKey = "server.urls";
    public const string DefaultUrl = "http://*:8080";

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your <see cref="IWebHostBuilder" />.
    /// </param>
    /// <returns>
    /// Your <see cref="IWebHostBuilder" />, now listening on port(s) found in the environment.
    /// </returns>
    public static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        return webHostBuilder.BindToPorts();
    }

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <returns>
    /// Your <see cref="WebApplicationBuilder" />, now listening on port(s) found in the environment.
    /// </returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br /> THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS.
    /// </remarks>
    public static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        webApplicationBuilder.WebHost.BindToPorts();
        return webApplicationBuilder;
    }

    internal static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, int? managementtHttpPort, int? managementHttpsPort)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);
        webApplicationBuilder.WebHost.BindToPorts(managementtHttpPort, managementHttpsPort);
        return webApplicationBuilder;
    }

    internal static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webhostBuilder, int? managementHttpPort, int? managementHttpsPort)
    {
        ArgumentGuard.NotNull(webhostBuilder);
        return webhostBuilder.BindToPorts(managementHttpPort, managementHttpsPort);
    }

    private static List<string> GetUrlsFromPorts(int? httpPort, int? httpsPort)
    {
        var urls = new List<string>();

        if (httpPort.HasValue)
        {
            urls.Add($"http://*:{httpPort}");
        }

        if (httpsPort.HasValue)
        {
            urls.Add($"https://*:{httpsPort}");
        }

        return urls;
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, int? managementHttpPort = null, int? managementHttpsPort = null)
    {
        var urls = new List<string>();

        string portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        string aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        string serverUrlSetting = webHostBuilder.GetSetting(DeprecatedServerUrlsKey); // check for deprecated setting
        string urlSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

        if (!string.IsNullOrEmpty(serverUrlSetting))
        {
            urls.Add(serverUrlSetting);
        }

        if (!string.IsNullOrEmpty(urlSetting))
        {
            urls.Add(urlSetting);
        }

        if (!string.IsNullOrWhiteSpace(portStr))
        {
            AddPortAndAspNetCoreUrls(urls, portStr, aspnetUrls);
        }
        else if (Platform.IsKubernetes)
        {
            AddFromKubernetesEnv(urls);
        }

        if (!urls.Any())
        {
            urls.Add(DefaultUrl);
        }

        urls.AddRange(GetUrlsFromPorts(managementHttpPort, managementHttpsPort));
        return webHostBuilder.BindToPorts(urls);
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, List<string> urls)
    {
        string currentSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
        var currentUrls = new HashSet<string>(urls);

        if (!string.IsNullOrEmpty(currentSetting))
        {
            currentUrls.UnionWith(currentSetting?.Split(';'));
        }

        return webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(";", currentUrls));
    }

    private static void AddPortAndAspNetCoreUrls(List<string> urls, string portStr, string aspnetUrls)
    {
        if (int.TryParse(portStr, out int port))
        {
            urls.Add($"http://*:{port}");
        }
        else if (portStr.Contains(';'))
        {
            if (!string.IsNullOrEmpty(aspnetUrls))
            {
                urls.AddRange(aspnetUrls.Split(';'));
            }
            else
            {
                string[] ports = portStr.Split(';');
                urls.Add($"http://*:{ports[0]}");
                urls.Add($"https://*:{ports[1]}");
            }
        }
    }

    private static void AddFromKubernetesEnv(List<string> urls)
    {
        string appName = Environment.GetEnvironmentVariable("HOSTNAME").Split('-')[0].ToUpperInvariant();
        string foundPort = Environment.GetEnvironmentVariable($"{appName}_SERVICE_PORT_HTTP");
        urls.Add($"http://*:{foundPort ?? "80"}");
    }
}

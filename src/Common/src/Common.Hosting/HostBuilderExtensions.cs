// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Common.Hosting;

public static class HostBuilderExtensions
{
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

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder)
    {
        var urls = new List<string>();

        string portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        string aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

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

        return webHostBuilder.BindToPorts(urls);
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, List<string> urls)
    {
        string currentSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
        List<string> currentUrls = currentSetting?.Split(';').ToList() ?? new List<string>();
        currentUrls.AddRange(urls);
        IEnumerable<string> distinctUrls = currentUrls.Distinct();
        return webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(";", distinctUrls));
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

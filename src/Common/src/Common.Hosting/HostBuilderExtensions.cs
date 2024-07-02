// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
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
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder UseCloudHosting(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        builder.BindToPorts();
        return builder;
    }

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder" /> to configure.
    /// </param>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br /> THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS.
    /// </remarks>
    /// <returns>
    /// The <see cref="WebApplicationBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        builder.WebHost.BindToPorts();
        return builder;
    }

    internal static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder builder, int? managementHttpPort, int? managementHttpsPort)
    {
        ArgumentGuard.NotNull(builder);
        builder.WebHost.BindToPorts(managementHttpPort, managementHttpsPort);
        return builder;
    }

    internal static IWebHostBuilder UseCloudHosting(this IWebHostBuilder builder, int? managementHttpPort, int? managementHttpsPort)
    {
        ArgumentGuard.NotNull(builder);
        return builder.BindToPorts(managementHttpPort, managementHttpsPort);
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

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder builder, int? managementHttpPort = null, int? managementHttpsPort = null)
    {
        var urls = new HashSet<string>();

        string portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        string aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        string serverUrlSetting = builder.GetSetting(DeprecatedServerUrlsKey); // check for deprecated setting
        string urlSetting = builder.GetSetting(WebHostDefaults.ServerUrlsKey);

        if (!string.IsNullOrEmpty(serverUrlSetting))
        {
            urls.Add(GetCanonical(serverUrlSetting));
        }

        if (!string.IsNullOrEmpty(urlSetting))
        {
            urls.Add(GetCanonical(urlSetting));
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

        foreach (string url in GetUrlsFromPorts(managementHttpPort, managementHttpsPort))
        {
            urls.Add(url);
        }

        return builder.BindToPorts(urls);
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder builder, HashSet<string> urls)
    {
        string currentSetting = builder.GetSetting(WebHostDefaults.ServerUrlsKey);

        if (!string.IsNullOrEmpty(currentSetting))
        {
            foreach (string url in currentSetting.Split(';'))
            {
                urls.Add(GetCanonical(url));
            }
        }

        return builder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(";", urls));
    }

    private static string GetCanonical(string serverUrlSetting)
    {
        string canonicalUrl = serverUrlSetting.Replace("0.0.0.0", "*", StringComparison.Ordinal);
        canonicalUrl = canonicalUrl.Replace("[::]", "*", StringComparison.Ordinal);
        return canonicalUrl;
    }

    private static void AddPortAndAspNetCoreUrls(HashSet<string> urls, string portStr, string aspnetUrls)
    {
        if (int.TryParse(portStr, CultureInfo.InvariantCulture, out int port))
        {
            urls.Add($"http://*:{port}");
        }
        else if (portStr?.Contains(';') == true)
        {
            if (!string.IsNullOrEmpty(aspnetUrls))
            {
                foreach (string url in aspnetUrls.Split(';'))
                {
                    urls.Add(GetCanonical(url));
                }
            }
            else
            {
                string[] ports = portStr.Split(';');
                urls.Add($"http://*:{ports[0]}");
                urls.Add($"https://*:{ports[1]}");
            }
        }
    }

    private static void AddFromKubernetesEnv(HashSet<string> urls)
    {
        string appName = Environment.GetEnvironmentVariable("HOSTNAME")?.Split('-')[0].ToUpperInvariant();
        string foundPort = Environment.GetEnvironmentVariable($"{appName}_SERVICE_PORT_HTTP");
        urls.Add($"http://*:{foundPort ?? "80"}");
    }
}

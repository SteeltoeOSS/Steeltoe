// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Hosting;

public static class HostBuilderExtensions
{
    public const string DEFAULT_URL = "http://*:8080";
    private const string DeprecatedServerUrlsKey = "server.urls";

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webHostBuilder">Your <see cref="IWebHostBuilder"/></param>
    /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally</param>
    /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally</param>
    /// <returns>Your <see cref="IWebHostBuilder"/>, now listening on port(s) found in the environment or passed in</returns>
    /// <remarks>runLocalPort parameter will not be used if an environment variable PORT is found</remarks>
    public static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (webHostBuilder == null)
        {
            throw new ArgumentNullException(nameof(webHostBuilder));
        }

        return webHostBuilder.BindToPorts(runLocalHttpPort, runLocalHttpsPort);
    }

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="hostBuilder">Your <see cref="IHostBuilder"/></param>
    /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally</param>
    /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally</param>
    /// <returns>Your <see cref="IHostBuilder"/>, now listening on port(s) found in the environment or passed in</returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br />
    /// THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS
    /// </remarks>
    public static IHostBuilder UseCloudHosting(this IHostBuilder hostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (hostBuilder == null)
        {
            throw new ArgumentNullException(nameof(hostBuilder));
        }

        return hostBuilder.ConfigureWebHost(configure => configure.BindToPorts(runLocalHttpPort, runLocalHttpsPort));
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webApplicationBuilder">Your <see cref="WebApplicationBuilder"/></param>
    /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally</param>
    /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally</param>
    /// <returns>Your <see cref="WebApplicationBuilder"/>, now listening on port(s) found in the environment or passed in</returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br />
    /// THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS
    /// </remarks>
    public static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (webApplicationBuilder == null)
        {
            throw new ArgumentNullException(nameof(webApplicationBuilder));
        }

        webApplicationBuilder.WebHost.BindToPorts(runLocalHttpPort, runLocalHttpsPort);
        return webApplicationBuilder;
    }
#endif

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort, int? runLocalHttpsPort)
    {
        var urls = new HashSet<string>();

        var portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        var aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        var serverUrlSetting = webHostBuilder.GetSetting(DeprecatedServerUrlsKey); // check for deprecated setting
        var urlSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

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
        else
        {
            AddRunLocalPorts(urls, runLocalHttpPort, runLocalHttpsPort);
        }

        if (urls.Any())
        {
            // setting ASPNETCORE_URLS should only be needed to override launchSettings.json
            if (string.IsNullOrWhiteSpace(portStr) && !Platform.IsKubernetes)
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Join(";", urls));
            }
        }
        else
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", DEFAULT_URL);
            urls.Add(DEFAULT_URL);
        }

        return webHostBuilder.BindToPorts(urls);
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, HashSet<string> urls)
    {
        string currentSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

        if (!string.IsNullOrEmpty(currentSetting))
        {
            foreach (var url in currentSetting.Split(';'))
            {
                urls.Add(GetCanonical(url));
            }
        }

        return webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(";", urls));
    }

    private static string GetCanonical(string serverUrlSetting)
    {
        var canonicalUrl = serverUrlSetting.Replace("0.0.0.0", "*", StringComparison.Ordinal);
        canonicalUrl = canonicalUrl.Replace("[::]", "*", StringComparison.Ordinal);
        return canonicalUrl;
    }

    private static void AddPortAndAspNetCoreUrls(HashSet<string> urls, string portStr, string aspnetUrls)
    {
        if (int.TryParse(portStr, out var port))
        {
            urls.Add($"http://*:{port}");
        }
        else if (portStr?.Contains(";") == true)
        {
            if (!string.IsNullOrEmpty(aspnetUrls))
            {
                foreach (var url in aspnetUrls.Split(';'))
                {
                   urls.Add(GetCanonical(url));
                }
            }
            else
            {
                var ports = portStr.Split(';');
                urls.Add($"http://*:{ports[0]}");
                urls.Add($"https://*:{ports[1]}");
            }
        }
    }

    private static void AddFromKubernetesEnv(HashSet<string> urls)
    {
        var appname = Environment.GetEnvironmentVariable("HOSTNAME")?.Split("-")[0].ToUpperInvariant();
        var foundPort = Environment.GetEnvironmentVariable(appname + "_SERVICE_PORT_HTTP");
        urls.Add($"http://*:{foundPort ?? "80"}");
    }

    private static void AddRunLocalPorts(HashSet<string> urls, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (runLocalHttpPort != null)
        {
            urls.Add($"http://*:{runLocalHttpPort}");
        }

        if (runLocalHttpsPort != null)
        {
            urls.Add($"https://*:{runLocalHttpsPort}");
        }
    }
}
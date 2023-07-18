// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Hosting;

public static class HostBuilderExtensions
{
    public const string DEFAULT_URL = "http://*:8080";
    internal const string DeprecatedServerUrlsKey = "server.urls";

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
        var serverUrlSetting = webHostBuilder.GetSetting(DeprecatedServerUrlsKey); // check for deprecated setting
        var urlSetting = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

        AddServerUrls(urlSetting, urls);
        AddServerUrls(serverUrlSetting, urls);

        if (!string.IsNullOrWhiteSpace(portStr))
        {
            AddPortAndAspNetCoreUrls(urls, portStr);
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

        urls = RemoveDuplicates(urls);

        return webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(";", urls));
    }

    private static HashSet<string> RemoveDuplicates(HashSet<string> urls)
    {
        HashSet<UrlEntry> entries = new ();
        HashSet<string> uniqueUrls = new HashSet<string>();

        foreach (var url in urls)
        {
            var bindingAddress = BindingAddress.Parse(url);
            var host = bindingAddress.Host;

            if (!IPAddress.TryParse(bindingAddress.Host, out var address) || address.ToString() == "::")
            {
                host = "*";
            }

            entries.Add(new UrlEntry() { Host = host, Scheme = bindingAddress.Scheme, Port = bindingAddress.Port });
        }

        foreach (IGrouping<int, UrlEntry> group in entries.GroupBy(entry => entry.Port))
        {
            var wildCardEntry = group.FirstOrDefault(entry => entry.Host == "*");
            if (!wildCardEntry.Equals(default(UrlEntry)))
            {
                uniqueUrls.Add(wildCardEntry.ToString());
            }
            else
            {
                foreach (var entry in group)
                {
                    uniqueUrls.Add(entry.ToString());
                }
            }
        }

        return uniqueUrls;
    }

    private struct UrlEntry
    {
        public string Scheme;
        public string Host;
        public int Port;

        public override string ToString()
        {
            return $"{Scheme}://{Host}:{Port}";
        }
    }

    private static void AddServerUrls(string serverUrlSetting, HashSet<string> urls)
    {
        if (!string.IsNullOrEmpty(serverUrlSetting))
        {
            foreach (var url in serverUrlSetting.Split(';'))
            {
                urls.Add(GetCanonical(url));
            }
        }
    }

    private static string GetCanonical(string serverUrlSetting)
    {
        var canonicalUrl = serverUrlSetting.Replace("0.0.0.0", "*", StringComparison.Ordinal);
        canonicalUrl = canonicalUrl.Replace("[::]", "*", StringComparison.Ordinal);
        return canonicalUrl;
    }

    private static void AddPortAndAspNetCoreUrls(HashSet<string> urls, string portStr)
    {
        if (int.TryParse(portStr, out var port))
        {
            urls.Add($"http://*:{port}");
        }
        else if (portStr?.Contains(";") == true)
        {
            var ports = portStr.Split(';');
            urls.Add($"http://*:{ports[0]}");
            urls.Add($"https://*:{ports[1]}");
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
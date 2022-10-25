// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.Hosting;

public static class HostBuilderExtensions
{
    public const string DefaultUrl = "http://*:8080";

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Specifically it adds ports and/or urls from the following
    /// environment variables: PORT, SERVERPORT, ASPNETCORE_URLS and in Kubernetes $"{appName}_SERVICE_PORT_HTTP") where appName is the prefix of HOSTNAME.
    /// Defaults to http://*:8080.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your <see cref="IWebHostBuilder" />.
    /// </param>
    /// <param name="runLocalHttpPort">
    /// Set the Http port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <param name="runLocalHttpsPort">
    /// Set the Https port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <returns>
    /// Your <see cref="IWebHostBuilder" />, now listening on port(s) found in the environment or passed in.
    /// </returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found.
    /// </remarks>
    public static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        return webHostBuilder.BindToPorts(runLocalHttpPort, runLocalHttpsPort, null);
    }

    internal static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder, Action<IWebHostBuilder, List<string>> configure)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        return webHostBuilder.BindToPorts(null, null, configure);
    }

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your <see cref="IHostBuilder" />.
    /// </param>
    /// <param name="runLocalHttpPort">
    /// Set the Http port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <param name="runLocalHttpsPort">
    /// Set the Https port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <returns>
    /// Your <see cref="IHostBuilder" />, now listening on port(s) found in the environment or passed in.
    /// </returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br /> THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS.
    /// </remarks>
    public static IHostBuilder UseCloudHosting(this IHostBuilder hostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureWebHost(configure => configure.BindToPorts(runLocalHttpPort, runLocalHttpsPort, null));
    }

    internal static IHostBuilder UseCloudHosting(this IHostBuilder hostBuilder, Action<IWebHostBuilder, List<string>> configureUrls)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureWebHost(configure => configure.BindToPorts(null, null, configureUrls));
    }

    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="runLocalHttpPort">
    /// Set the Http port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <param name="runLocalHttpsPort">
    /// Set the Https port number with code so you don't need to set environment variables locally.
    /// </param>
    /// <returns>
    /// Your <see cref="WebApplicationBuilder" />, now listening on port(s) found in the environment or passed in.
    /// </returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br /> THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS.
    /// </remarks>
    public static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, int? runLocalHttpPort = null,
        int? runLocalHttpsPort = null)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        webApplicationBuilder.WebHost.BindToPorts(runLocalHttpPort, runLocalHttpsPort, null);
        return webApplicationBuilder;
    }

    internal static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, Action<IWebHostBuilder, List<string>> configure)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        webApplicationBuilder.WebHost.BindToPorts(null, null, configure);
        return webApplicationBuilder;
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort, int? runLocalHttpsPort,
        Action<IWebHostBuilder, List<string>> configure)
    {
        var urls = new List<string>();

        string portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        string aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

        // if runLocalPorts are provided, ignore the AspnetCoreUrl setting locally
        if (!AddRunLocalPorts(urls, runLocalHttpPort, runLocalHttpsPort) && !string.IsNullOrEmpty(aspnetUrls))
        {
            AddAspNetCoreUrls(urls, aspnetUrls);
        }

        // Continue to respect any PORT settings 
        if (!string.IsNullOrWhiteSpace(portStr))
        {
            AddPortAndAspNetCoreUrls(urls, portStr, aspnetUrls);
        }
        // or any K8s Environment settings
        else if (Platform.IsKubernetes)
        {
            AddFromKubernetesEnv(urls);
        }

        // if no URLS are specified in any source, use the default 
        if (!urls.Any())
        {
            urls.Add(DefaultUrl);
        }

        configure?.Invoke(webHostBuilder, urls); // Could conditionally add the Management:Port setting

        // Set ASPNETCORE_URLS for local 
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Join(";", urls));

        webHostBuilder.UseUrls(urls.ToArray());

        return webHostBuilder;
    }

    private static void AddPortAndAspNetCoreUrls(List<string> urls, string portStr, string aspnetUrls)
    {
        if (int.TryParse(portStr, out int port))
        {
            urls.Add($"http://*:{port}");
        }
        else if (portStr.Contains(";"))
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

    private static void AddAspNetCoreUrls(List<string> urls, string aspnetUrls)
    {
        if (!string.IsNullOrEmpty(aspnetUrls))
        {
            urls.AddRange(aspnetUrls.Split(';'));
        }
    }

    private static void AddFromKubernetesEnv(List<string> urls)
    {
        string appName = Environment.GetEnvironmentVariable("HOSTNAME").Split("-")[0].ToUpperInvariant();
        string foundPort = Environment.GetEnvironmentVariable($"{appName}_SERVICE_PORT_HTTP");
        urls.Add($"http://*:{foundPort ?? "80"}");
    }

    private static bool AddRunLocalPorts(List<string> urls, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (runLocalHttpPort != null)
        {
            urls.Add($"http://*:{runLocalHttpPort}");
        }

        if (runLocalHttpsPort != null)
        {
            urls.Add($"https://*:{runLocalHttpsPort}");
        }

        return runLocalHttpPort != null || runLocalHttpsPort != null;
    }
}

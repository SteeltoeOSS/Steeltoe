// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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

    internal static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder, Func<IWebHostBuilder, Tuple<int?,bool>> configure)
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

    internal static IHostBuilder UseCloudHosting(this IHostBuilder hostBuilder, Func<IWebHostBuilder, Tuple<int?, bool>> configurePorts)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureWebHost(configure => configure.BindToPorts(null, null, configurePorts));
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

    internal static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, Func<IWebHostBuilder, Tuple<int?, bool>> configure)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        webApplicationBuilder.WebHost.BindToPorts(null, null, configure);
        return webApplicationBuilder;
    }

    private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder,
        int? runLocalHttpPort,
        int? runLocalHttpsPort,
        Func<IWebHostBuilder, Tuple<int?, bool>> configure)
    {
        var urls = new List<string>();

        string portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
        string aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

        //AddRunLocalPorts(urls, runLocalHttpPort, runLocalHttpsPort);
        var httpPorts = new List<int>();
        var httpsPorts = new List<int>();
        if (runLocalHttpPort.HasValue && !httpPorts.Contains(runLocalHttpPort.Value))
        {
            httpPorts.Add(runLocalHttpPort.Value);
        }
        if (runLocalHttpsPort.HasValue && !httpsPorts.Contains(runLocalHttpsPort.Value))
        {
            httpsPorts.Add(runLocalHttpsPort.Value);
        }
      

        // Tye PORTS
        if (!string.IsNullOrWhiteSpace(portStr))
        {
            AddPortAndAspNetCoreUrls(httpPorts, httpsPorts, portStr, aspnetUrls);
        }
        // or any K8s Environment settings
        else if (Platform.IsKubernetes)
        {
            var k8sPort = GetPortFromKubernetesEnv();
            if (k8sPort.HasValue)
            {
                httpPorts.Add(k8sPort.Value);
            }
        }

        
        var (managementPort, isHttps) = configure?.Invoke(webHostBuilder) ?? new Tuple<int?, bool>(null, false); // Could conditionally add the Management:Port setting
      
        webHostBuilder.ConfigureKestrel(options =>
        {
            if (!httpPorts.Any() && !httpsPorts.Any())
            {
                httpPorts.Add(8080);
            }
            if (managementPort != null)
            {
                if (isHttps)
                {
                    options.ListenAnyIP(managementPort.Value, opt => opt.UseHttps());
                   
                }
                else
                {
                    options.ListenAnyIP(managementPort.Value);
                }
            }

            foreach (var port in httpPorts)
            {
                options.ListenAnyIP(port);
            }
            foreach (var port in httpsPorts)
            {
                options.ListenAnyIP(port, opt => opt.UseHttps());
            }
        });
        return webHostBuilder;
    }

    private static void GetPortsFromUrls(List<int> httpPorts, List<int> httpsPorts, IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            var uri = new Uri(url.Replace("*", "anyhost"));
            if (uri.Scheme == "https")
            {
                httpsPorts.Add(uri.Port);
            }
            else
            {
                httpPorts.Add(uri.Port);
            }
        }
    }

    private static void AddPortAndAspNetCoreUrls(List<int> httpPorts, List<int> httpsPorts, string portStr, string aspnetUrls)
    {
        if (int.TryParse(portStr, out int port))
        {
            httpPorts.Add(port);
        }
        else if (portStr.Contains(";"))
        {
            if (!string.IsNullOrEmpty(aspnetUrls))
            {
                GetPortsFromUrls(httpPorts, httpsPorts, aspnetUrls.Split(';'));
            }
            else
            {
                string[] ports = portStr.Split(';');
                if (ports.Length > 0 && int.TryParse(ports[0], out var httpPort))
                {
                    httpPorts.Add(httpPort);
                }
                if (ports.Length > 1 && int.TryParse(ports[1], out var httpsPort))
                {
                    httpsPorts.Add(httpsPort);
                }
            }
        }
    }

    private static int? GetPortFromKubernetesEnv()
    {
        string appName = Environment.GetEnvironmentVariable("HOSTNAME").Split("-")[0].ToUpperInvariant();
        string foundPort = Environment.GetEnvironmentVariable($"{appName}_SERVICE_PORT_HTTP");
        return int.TryParse(foundPort, out var port) ? port : null;
    }
  
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Management.Endpoint.ManagementPort;

internal static class ManagementPortWebHostBuilderExtensions
{
    private const string ManagementPortKey = "management:endpoints:port";
    private const string ManagementSslKey = "management:endpoints:sslenabled";

    public static IWebHostBuilder AddManagementPort(this IWebHostBuilder webHostBuilder)
    {
        (int? httpPort, int? httpsPort) = GetManagementPorts(webHostBuilder);

        if (httpPort.HasValue || httpsPort.HasValue)
        {
            webHostBuilder.UseCloudHosting(httpPort, httpsPort);
        }

        return webHostBuilder;
    }

    private static (int? HttpPort, int? HttpsPort) GetManagementPorts(this IWebHostBuilder webHostBuilder)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        string? portSetting = webHostBuilder.GetSetting(ManagementPortKey);
        string? sslSetting = webHostBuilder.GetSetting(ManagementSslKey);

        int? httpPort = null;
        int? httpsPort = null;

        if (string.IsNullOrEmpty(portSetting))
        {
            IConfiguration? configuration = GetConfigurationFallback(); // try reading directly from appsettings.json
            portSetting = configuration?[ManagementPortKey];
            sslSetting = configuration?[ManagementSslKey];
        }

        if (int.TryParse(portSetting, CultureInfo.InvariantCulture, out int managementPort) && managementPort > 0)
        {
            if (bool.TryParse(sslSetting, out bool enableSsl) && enableSsl)
            {
                httpsPort = managementPort;
            }
            else
            {
                httpPort = managementPort;
            }
        }

        return (httpPort, httpsPort);
    }

    private static IConfiguration? GetConfigurationFallback()
    {
        IConfiguration? configuration = null;

        try
        {
            string? environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment != null)
            {
                configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environment}.json", true).Build();
            }
        }
        catch (Exception)
        {
            // Not much we can do ...
        }

        return configuration;
    }
}

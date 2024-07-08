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
    private const string ManagementPortKey = "Management:Endpoints:Port";
    private const string ManagementSslKey = "Management:Endpoints:SslEnabled";

    public static IWebHostBuilder AddManagementPort(this IWebHostBuilder builder)
    {
        (int? httpPort, int? httpsPort) = GetManagementPorts(builder);

        if (httpPort.HasValue || httpsPort.HasValue)
        {
            builder.UseCloudHosting(httpPort, httpsPort);
        }

        return builder;
    }

    private static (int? HttpPort, int? HttpsPort) GetManagementPorts(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        string? portSetting = builder.GetSetting(ManagementPortKey);
        string? sslSetting = builder.GetSetting(ManagementSslKey);

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

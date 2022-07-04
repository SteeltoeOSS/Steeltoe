// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Security.DataProtection.CredHub;

public static class CredHubHostBuilderExtensions
{
    /// <summary>
    /// Reach out to a CredHub server to interpolate credentials found in VCAP_SERVICES.
    /// </summary>
    /// <param name="webHostBuilder">Your app's host builder.</param>
    /// <param name="loggerFactory">To enable logging in the credhub client, pass in a loggerfactory.</param>
    /// <returns>Your application's host builder with credentials interpolated.</returns>
    public static IWebHostBuilder UseCredHubInterpolation(this IWebHostBuilder webHostBuilder, ILoggerFactory loggerFactory = null)
    {
        ILogger startupLogger = null;
        ILogger credhubLogger = null;
        if (loggerFactory != null)
        {
            startupLogger = loggerFactory.CreateLogger("Steeltoe.Security.DataProtection.CredHubCore");
            credhubLogger = loggerFactory.CreateLogger<CredHubClient>();
        }

        var vcapServices = Environment.GetEnvironmentVariable("VCAP_SERVICES");

        // don't bother interpolating if there aren't any credhub references
        if (vcapServices != null && vcapServices.Contains("credhub-ref"))
        {
            webHostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                var builtConfig = config.Build();
                CredHubClient credHubClient;

                var credHubOptions = builtConfig.GetSection("CredHubClient").Get<CredHubOptions>();
                credHubOptions.Validate();
                try
                {
                    startupLogger?.LogTrace("Using UAA auth for CredHub client with client id {ClientId}", credHubOptions.ClientId);
                    credHubClient = CredHubClient.CreateUaaClientAsync(credHubOptions, credhubLogger).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    startupLogger?.LogCritical(e, "Failed to initialize CredHub client");

                    // return early to prevent call we know will fail
                    return;
                }

                try
                {
                    // send the interpolate request to CredHub
                    var interpolated = credHubClient.InterpolateServiceDataAsync(vcapServices).GetAwaiter().GetResult();

                    // update the environment variable for this process
                    Environment.SetEnvironmentVariable("VCAP_SERVICES", interpolated);
                }
                catch (Exception e)
                {
                    startupLogger?.LogCritical(e, "Failed to interpolate service data with CredHub");
                }
            });
        }
        else
        {
            startupLogger?.LogInformation("No CredHub references found in VCAP_SERVICES");
        }

        return webHostBuilder;
    }
}

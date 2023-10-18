// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class ConfigureSpringBootAdminClientOptions : IConfigureOptionsWithKey<SpringBootAdminClientOptions>
{
    private const string ManagementInfoPrefix = "spring:boot:admin:client";

    private readonly IConfiguration _configuration;
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    public string ConfigurationKey => ManagementInfoPrefix;

    public ConfigureSpringBootAdminClientOptions(IConfiguration configuration, IApplicationInstanceInfo applicationInstanceInfo)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(applicationInstanceInfo);

        _configuration = configuration;
        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public void Configure(SpringBootAdminClientOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        // Require base path to be supplied directly, in the configuration, or in the app instance info
        options.BasePath ??= GetBasePath() ?? _applicationInstanceInfo.Uris?.FirstOrDefault() ??
            throw new InvalidOperationException($"Please set {ManagementInfoPrefix}:BasePath in order to register with Spring Boot Admin");

        options.ApplicationName ??= _applicationInstanceInfo.GetApplicationNameInContext(SteeltoeComponent.Management);
    }

    private string? GetBasePath()
    {
        string? urlString = _configuration.GetValue<string?>("URLS");

        if (urlString != null)
        {
            string[] urls = urlString.Split(';');

            if (urls.Length > 0)
            {
                return urls[0];
            }
        }

        return null;
    }
}

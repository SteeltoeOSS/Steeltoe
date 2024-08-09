// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Http;
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
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);

        _configuration = configuration;
        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public void Configure(SpringBootAdminClientOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.BasePath ??= GetBasePath() ??
            throw new InvalidOperationException($"Please set {ManagementInfoPrefix}:BasePath in order to register with Spring Boot Admin");

        options.ApplicationName ??= _applicationInstanceInfo.ApplicationName;
    }

    private string? GetBasePath()
    {
        ICollection<string> listenAddresses = _configuration.GetListenAddresses();

        if (listenAddresses.Count == 1 && listenAddresses.ElementAt(0) == "http://localhost:5000")
        {
            // Nothing was configured, so we got the implicit default.
            return null;
        }

        BindingAddress[] addresses = listenAddresses.Select(BindingAddress.Parse).ToArray();

        foreach (BindingAddress address in addresses)
        {
            if (!string.IsNullOrEmpty(address.Host) && !address.Host.Contains('+') && !address.Host.Contains('*'))
            {
#pragma warning disable S4040 // Strings should be normalized to uppercase
                return
                    $"{address.Scheme.ToLowerInvariant()}{Uri.SchemeDelimiter}{address.Host.ToLowerInvariant()}:{address.Port.ToString(CultureInfo.InvariantCulture)}";
#pragma warning restore S4040 // Strings should be normalized to uppercase
            }
        }

        return null;
    }
}

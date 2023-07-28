// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Options;

internal sealed class ConfigureManagementOptions : IConfigureOptions<ManagementOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints";
    private const string CloudFoundryEnabledPrefix = "management:cloudfoundry:enabled";
    private const string DefaultPath = "/actuator";
    internal const string DefaultCloudFoundryPath = "/cloudfoundryapplication";

    private readonly IConfiguration _configuration;

    public ConfigureManagementOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(ManagementOptions options)
    {
        ArgumentGuard.NotNull(options);

        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.IsCloudFoundryEnabled = !bool.TryParse(_configuration[CloudFoundryEnabledPrefix], out bool enabled) || enabled; // Default true

        foreach (string converterTypeName in options.CustomJsonConverters ?? Array.Empty<string>())
        {
            var converterType = Type.GetType(converterTypeName);

            if (converterType != null)
            {
                var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                options.SerializerOptions.Converters.Add(converterInstance);
            }
        }

        options.Path ??= DefaultPath;

        options.Exposure = new Exposure(_configuration);
    }
}

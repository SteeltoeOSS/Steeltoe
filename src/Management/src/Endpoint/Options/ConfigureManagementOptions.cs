// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Options;

internal sealed class ConfigureManagementOptions : IConfigureOptionsWithKey<ManagementOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints";
    private const string CloudFoundryEnabledPrefix = "management:cloudfoundry:enabled";
    private const string DefaultPath = "/actuator";
    internal const string DefaultCloudFoundryPath = "/cloudfoundryapplication";

    private readonly IConfiguration _configuration;

    public string ConfigurationKey => ManagementInfoPrefix;

    public ConfigureManagementOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(ManagementOptions options)
    {
        ArgumentGuard.NotNull(options);

        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.IsCloudFoundryEnabled = true;

        if (bool.TryParse(_configuration[CloudFoundryEnabledPrefix], out bool isEnabled))
        {
            options.IsCloudFoundryEnabled = isEnabled;
        }

        ConfigureSerializerOptions(options);

        options.Path ??= DefaultPath;

        options.Exposure ??= new Exposure();

        var configureExposure = new ConfigureExposure(_configuration);
        configureExposure.Configure(options.Exposure);
    }

    private static void ConfigureSerializerOptions(ManagementOptions options)
    {
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        foreach (string converterTypeName in options.CustomJsonConverters)
        {
            var converterType = Type.GetType(converterTypeName, true)!;
            var converterInstance = (JsonConverter)Activator.CreateInstance(converterType)!;

            options.SerializerOptions.Converters.Add(converterInstance);
        }

        if (!options.SerializerOptions.Converters.Any(converter => converter is JsonStringEnumConverter))
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        if (!options.SerializerOptions.Converters.Any(converter => converter is HealthConverter or HealthConverterV3))
        {
            options.SerializerOptions.Converters.Add(new HealthConverter());
        }

        if (!options.SerializerOptions.Converters.Any(converter => converter is HttpTraceResultConverter))
        {
            options.SerializerOptions.Converters.Add(new HttpTraceResultConverter());
        }

        if (!options.SerializerOptions.Converters.Any(c => c is ServiceDescriptorConverter))
        {
            options.SerializerOptions.Converters.Add(new ServiceDescriptorConverter());
        }
    }

    private sealed class ConfigureExposure
    {
        private const string Prefix = "management:endpoints:actuator:exposure";
        private const string SecondChancePrefix = "management:endpoints:web:exposure";

        private static readonly List<string> DefaultIncludes = new()
        {
            "health",
            "info"
        };

        private readonly IConfiguration _configuration;

        public ConfigureExposure(IConfiguration configuration)
        {
            ArgumentGuard.NotNull(configuration);

            _configuration = configuration;
        }

        public void Configure(Exposure options)
        {
            ArgumentGuard.NotNull(options);

            _configuration.GetSection(Prefix).Bind(options);

            IConfigurationSection secondSection = _configuration.GetSection(SecondChancePrefix);

            if (secondSection.Exists())
            {
                options.Include = GetListFromConfigurationCsvString(secondSection, "include") ?? new List<string>();
                options.Exclude = GetListFromConfigurationCsvString(secondSection, "exclude") ?? new List<string>();
            }

            if (options.Include.Count == 0 && options.Exclude.Count == 0)
            {
                options.Include = DefaultIncludes;
            }
        }

        private static List<string>? GetListFromConfigurationCsvString(IConfigurationSection section, string key)
        {
            return section.GetValue<string?>(key)?.Split(',').ToList();
        }
    }
}

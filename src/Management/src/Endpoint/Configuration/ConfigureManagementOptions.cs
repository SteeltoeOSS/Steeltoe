// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Json;

namespace Steeltoe.Management.Endpoint.Configuration;

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
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(ManagementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.IsCloudFoundryEnabled = true;

        if (bool.TryParse(_configuration[CloudFoundryEnabledPrefix], out bool isEnabled))
        {
            options.IsCloudFoundryEnabled = isEnabled;
        }

        ConfigureSerializerOptions(options);

        options.Path ??= DefaultPath;

        var configureExposure = new ConfigureExposure(_configuration);
        configureExposure.Configure(options.Exposure);
    }

    private static void ConfigureSerializerOptions(ManagementOptions options)
    {
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // This was added initially for the route mappings actuator, to make generic method signatures human-readable,
        // but may affect other endpoints too. Removing this is a breaking change.
        options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        options.SerializerOptions.AddJsonIgnoreEmptyCollection();

        foreach (string converterTypeName in options.CustomJsonConverters)
        {
            var converterType = Type.GetType(converterTypeName, true)!;
            var converterInstance = (JsonConverter)Activator.CreateInstance(converterType)!;

            options.SerializerOptions.Converters.Add(converterInstance);
        }

        if (!options.SerializerOptions.Converters.OfType<JsonStringEnumConverter>().Any())
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }
    }

    private sealed class ConfigureExposure
    {
        private const string Prefix = "management:endpoints:actuator:exposure";
        private const string SpringKeyPrefix = "management:endpoints:web:exposure";

        private static readonly List<string> DefaultIncludes =
        [
            "health",
            "info"
        ];

        private readonly IConfiguration _configuration;

        public ConfigureExposure(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _configuration = configuration;
        }

        public void Configure(Exposure options)
        {
            ArgumentNullException.ThrowIfNull(options);

            IConfigurationSection springSection = _configuration.GetSection(SpringKeyPrefix);

            if (springSection.Exists())
            {
                List<string> springIncludes = GetListFromConfigurationCsvString(springSection, "include") ?? [];
                ReplaceCollection(options.Include, springIncludes);

                List<string> springExcludes = GetListFromConfigurationCsvString(springSection, "exclude") ?? [];
                ReplaceCollection(options.Exclude, springExcludes);
            }

            _configuration.GetSection(Prefix).Bind(options);

            if (options.Include.Count == 0 && options.Exclude.Count == 0)
            {
                ReplaceCollection(options.Include, DefaultIncludes);
            }
            else
            {
                if (options.Include is [""])
                {
                    ReplaceCollection(options.Include, Array.Empty<string>());
                }

                if (options.Exclude is [""])
                {
                    ReplaceCollection(options.Exclude, Array.Empty<string>());
                }
            }
        }

        private static List<string>? GetListFromConfigurationCsvString(IConfigurationSection section, string key)
        {
            return section.GetValue<string?>(key)?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static void ReplaceCollection(ICollection<string> source, IEnumerable<string> itemsToAdd)
        {
            source.Clear();

            foreach (string item in itemsToAdd)
            {
                source.Add(item);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

internal sealed class CloudFoundryServiceBindingConfigurationProvider : PostProcessorConfigurationProvider
{
    public static readonly string FromKeyPrefix = ConfigurationPath.Combine("vcap", "services");
    public static readonly string ToKeyPrefix = ConfigurationPath.Combine("steeltoe", "service-bindings");

    private readonly IServiceBindingsReader _serviceBindingsReader;

    public CloudFoundryServiceBindingConfigurationProvider(PostProcessorConfigurationSource source, IServiceBindingsReader serviceBindingsReader)
        : base(source)
    {
        ArgumentNullException.ThrowIfNull(serviceBindingsReader);

        _serviceBindingsReader = serviceBindingsReader;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? json = _serviceBindingsReader.GetServiceBindingsJson();

        if (!string.IsNullOrEmpty(json))
        {
            IConfigurationRoot configurationRoot = BuildConfiguration(json);
            LoadSections("vcap:services", configurationRoot.GetChildren(), data);
        }

        Data = data;
        PostProcessConfiguration();
        OnReload();
    }

    private static IConfigurationRoot BuildConfiguration(string json)
    {
        using MemoryStream stream = GetStream(json);
        var builder = new ConfigurationBuilder();
        builder.Add(new JsonStreamConfigurationSource(stream));
        return builder.Build();
    }

    private static MemoryStream GetStream(string json)
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        {
            textWriter.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private void LoadSections(string prefix, IEnumerable<IConfigurationSection> sections, IDictionary<string, string?> data)
    {
        foreach (IConfigurationSection section in sections)
        {
            LoadSection(prefix, section, data);
            LoadSections(prefix, section.GetChildren(), data);
        }
    }

    private void LoadSection(string prefix, IConfigurationSection section, IDictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(section.Value))
        {
            return;
        }

        string key = $"{prefix}{ConfigurationPath.KeyDelimiter}{section.Path}";

        if (!Source.IgnoreKeyPredicate(key))
        {
            data[key] = section.Value;
        }
    }
}

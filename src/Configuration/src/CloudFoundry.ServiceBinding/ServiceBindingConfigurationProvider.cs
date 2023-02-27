// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class ServiceBindingConfigurationProvider : PostProcessorConfigurationProvider
{
    public static readonly string InputKeyPrefix = ConfigurationPath.Combine("vcap", "services");
    public static readonly string OutputKeyPrefix = ConfigurationPath.Combine("steeltoe", "service-bindings");

    private readonly IServiceBindingsReader _serviceBindingsReader;

    public ServiceBindingConfigurationProvider(PostProcessorConfigurationSource source, IServiceBindingsReader serviceBindingsReader)
        : base(source)
    {
        ArgumentGuard.NotNull(source);
        ArgumentGuard.NotNull(serviceBindingsReader);

        _serviceBindingsReader = serviceBindingsReader;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string json = _serviceBindingsReader.GetServiceBindingsJson();

        if (!string.IsNullOrEmpty(json))
        {
            IConfigurationRoot configurationRoot = BuildConfiguration(json);
            LoadSections("vcap:services", configurationRoot.GetChildren(), data);
        }

        Data = data;
        PostProcessConfiguration();
    }

    private static IConfigurationRoot BuildConfiguration(string json)
    {
        using Stream stream = GetStream(json);
        var builder = new ConfigurationBuilder();
        builder.Add(new JsonStreamConfigurationSource(stream));
        return builder.Build();
    }

    private static Stream GetStream(string json)
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        {
            textWriter.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private void LoadSections(string prefix, IEnumerable<IConfigurationSection> sections, IDictionary<string, string> data)
    {
        foreach (IConfigurationSection section in sections)
        {
            LoadSection(prefix, section, data);
            LoadSections(prefix, section.GetChildren(), data);
        }
    }

    private void LoadSection(string prefix, IConfigurationSection section, IDictionary<string, string> data)
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

    protected override void PostProcessConfiguration()
    {
        if (this.IsCloudFoundryBindingsEnabled())
        {
            base.PostProcessConfiguration();
        }
    }
}

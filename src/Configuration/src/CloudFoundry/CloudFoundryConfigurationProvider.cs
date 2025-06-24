// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class CloudFoundryConfigurationProvider : ConfigurationProvider
{
    private readonly ICloudFoundrySettingsReader _settingsReader;

    internal IDictionary<string, string?> Properties => Data;

    public CloudFoundryConfigurationProvider(ICloudFoundrySettingsReader settingsReader)
    {
        ArgumentNullException.ThrowIfNull(settingsReader);

        _settingsReader = settingsReader;
    }

    public override void Load()
    {
        Process();
    }

    internal static Stream GetStream(string json)
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        {
            textWriter.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private void AddDiegoVariables(Dictionary<string, string?> data)
    {
        if (!data.ContainsKey("vcap:application:instance_id"))
        {
            data["vcap:application:instance_id"] = !string.IsNullOrEmpty(_settingsReader.InstanceId) ? _settingsReader.InstanceId : "-1";
        }

        if (!data.ContainsKey("vcap:application:instance_index"))
        {
            data["vcap:application:instance_index"] = !string.IsNullOrEmpty(_settingsReader.InstanceIndex) ? _settingsReader.InstanceIndex : "-1";
        }

        if (!data.ContainsKey("vcap:application:port"))
        {
            data["vcap:application:port"] = !string.IsNullOrEmpty(_settingsReader.InstancePort) ? _settingsReader.InstancePort : "-1";
        }

        data["vcap:application:instance_ip"] = _settingsReader.InstanceIP;
        data["vcap:application:internal_ip"] = _settingsReader.InstanceInternalIP;
    }

    private void Process()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        string? applicationJson = _settingsReader.ApplicationJson;

        if (!string.IsNullOrEmpty(applicationJson))
        {
            using Stream stream = GetStream(applicationJson);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(stream));
            IConfigurationRoot applicationData = builder.Build();

            LoadData("vcap:application", applicationData.GetChildren(), data);
            AddDiegoVariables(data);

            // Enable evaluation of X-Forwarded headers so that ASP.NET Core works automatically behind Gorouter.
            // Equivalent to setting ASPNETCORE_FORWARDEDHEADERS_ENABLED to true.
            data["FORWARDEDHEADERS_ENABLED"] = "true";
        }

        string? servicesJson = _settingsReader.ServicesJson;

        if (!string.IsNullOrEmpty(servicesJson))
        {
            using Stream stream = GetStream(servicesJson);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(stream));
            IConfigurationRoot servicesData = builder.Build();

            LoadData("vcap:services", servicesData.GetChildren(), data);
        }

        Data = data;
    }

    private void LoadData(string prefix, IEnumerable<IConfigurationSection> sections, Dictionary<string, string?> data)
    {
        foreach (IConfigurationSection section in sections)
        {
            LoadSection(prefix, section, data);
            LoadData(prefix, section.GetChildren(), data);
        }
    }

    private void LoadSection(string prefix, IConfigurationSection section, Dictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(section.Value))
        {
            return;
        }

        string key = ConfigurationPath.Combine(prefix, section.Path);
        data[key] = section.Value;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public class CloudFoundryConfigurationProvider : ConfigurationProvider
{
    private readonly ICloudFoundrySettingsReader _settingsReader;

    internal IDictionary<string, string> Properties => Data;

    public CloudFoundryConfigurationProvider(ICloudFoundrySettingsReader settingsReader)
    {
        ArgumentGuard.NotNull(settingsReader);

        _settingsReader = settingsReader;
    }

    public override void Load()
    {
        Process();
    }

    internal static Stream GetMemoryStream(string json)
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        {
            textWriter.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    internal void AddDiegoVariables(IDictionary<string, string> data)
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
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string appJson = _settingsReader.ApplicationJson;

        if (!string.IsNullOrEmpty(appJson))
        {
            using Stream stream = GetMemoryStream(appJson);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(stream));
            IConfigurationRoot applicationData = builder.Build();

            if (applicationData != null)
            {
                LoadData("vcap:application", applicationData.GetChildren(), data);
                AddDiegoVariables(data);
            }
        }

        string appServicesJson = _settingsReader.ServicesJson;

        if (!string.IsNullOrEmpty(appServicesJson))
        {
            using Stream stream = GetMemoryStream(appServicesJson);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(stream));
            IConfigurationRoot servicesData = builder.Build();

            if (servicesData != null)
            {
                LoadData("vcap:services", servicesData.GetChildren(), data);
            }
        }

        Data = data;
    }

    private void LoadData(string prefix, IEnumerable<IConfigurationSection> sections, IDictionary<string, string> data)
    {
        if (sections != null)
        {
            foreach (IConfigurationSection section in sections)
            {
                LoadSection(prefix, section, data);
                LoadData(prefix, section.GetChildren(), data);
            }
        }
    }

    private void LoadSection(string prefix, IConfigurationSection section, IDictionary<string, string> data)
    {
        if (section == null || string.IsNullOrEmpty(section.Value))
        {
            return;
        }

        data[prefix + ConfigurationPath.KeyDelimiter + section.Path] = section.Value;
    }
}

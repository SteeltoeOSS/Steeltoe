// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryConfigurationProvider : ConfigurationProvider
    {
        private readonly ICloudFoundrySettingsReader _settingsReader;

        public CloudFoundryConfigurationProvider(ICloudFoundrySettingsReader settingsReader)
        {
            _settingsReader = settingsReader ?? throw new ArgumentNullException(nameof(settingsReader));
        }

        internal IDictionary<string, string> Properties => Data;

        public override void Load()
        {
            Process();
        }

        internal static MemoryStream GetMemoryStream(string json)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(json);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
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

            data["vcap:application:instance_ip"] = _settingsReader.InstanceIp;
            data["vcap:application:internal_ip"] = _settingsReader.InstanceInternalIp;
        }

        private void Process()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var appJson = _settingsReader.ApplicationJson;
            if (!string.IsNullOrEmpty(appJson))
            {
                var memStream = GetMemoryStream(appJson);
                var builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                var applicationData = builder.Build();

                if (applicationData != null)
                {
                    LoadData("vcap:application", applicationData.GetChildren(), data);
                    AddDiegoVariables(data);
                }
            }

            var appServicesJson = _settingsReader.ServicesJson;
            if (!string.IsNullOrEmpty(appServicesJson))
            {
                var memStream = GetMemoryStream(appServicesJson);
                var builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                var servicesData = builder.Build();

                if (servicesData != null)
                {
                    LoadData("vcap:services", servicesData.GetChildren(), data);
                }
            }

            Data = data;
        }

        private void LoadData(string prefix, IEnumerable<IConfigurationSection> sections, IDictionary<string, string> data)
        {
            if (sections == null || !sections.Any())
            {
                return;
            }

            foreach (var section in sections)
            {
                LoadSection(prefix, section, data);
                LoadData(prefix, section.GetChildren(), data);
            }
        }

        private void LoadSection(string prefix, IConfigurationSection section, IDictionary<string, string> data)
        {
            if (section == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(section.Value))
            {
                return;
            }

            data[prefix + ConfigurationPath.KeyDelimiter + section.Path] = section.Value;
        }
    }
}

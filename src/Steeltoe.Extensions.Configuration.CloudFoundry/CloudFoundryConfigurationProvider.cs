//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryConfigurationProvider : ConfigurationProvider
    {
        private readonly ICloudFoundrySettingsReader _settingsReader;

        public CloudFoundryConfigurationProvider(ICloudFoundrySettingsReader settingsReader)
        {
            if (settingsReader == null)
            {
                throw new ArgumentNullException(nameof(settingsReader));
            }

            this._settingsReader = settingsReader;
        }

        public override void Load()
        {
            this.Process();
        }

        private void Process()
        {
            string appJson = this._settingsReader.ApplicationJson;
            if (!string.IsNullOrEmpty(appJson))
            {

                var memStream = GetMemoryStream(appJson);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                var applicationData = builder.Build();

                if (applicationData != null)
                {
                    LoadData("vcap:application", applicationData.GetChildren());

                    string vcapAppName = Data["vcap:application:name"];
                    if (!string.IsNullOrEmpty(vcapAppName))
                    {
                        Data["spring:application:name"] = vcapAppName;
                    }

                    AddDiegoVariables();
                }
            }

            string appServicesJson = this._settingsReader.ServicesJson;
            if (!string.IsNullOrEmpty(appServicesJson))
            {
                var memStream = GetMemoryStream(appServicesJson);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                var servicesData = builder.Build();

                if (servicesData != null)
                {
                    LoadData("vcap:services", servicesData.GetChildren());
                }
            }

        }

        internal void AddDiegoVariables()
        {
            if (!Data.ContainsKey("vcap:application:instance_id"))
            {
                Data["vcap:application:instance_id"] = this._settingsReader.InstanceId;
            }

            if (!Data.ContainsKey("vcap:application:instance_index"))
            {
                Data["vcap:application:instance_index"] = this._settingsReader.InstanceIndex;
            }

            if (!Data.ContainsKey("vcap:application:port"))
            {
                Data["vcap:application:port"] = this._settingsReader.InstancePort;
            }

            Data["vcap:application:instance_ip"] = this._settingsReader.InstanceIp;
            Data["vcap:application:internal_ip"] = this._settingsReader.InstanceInternalIp;

        }
        internal IDictionary<string, string> Properties
        {
            get
            {
                return Data;
            }
        }

        private void LoadData(string prefix, IEnumerable<IConfigurationSection> sections)
        {
            if (sections == null || sections.Count() == 0)
            {
                return;
            }
            foreach (IConfigurationSection section in sections)
            {
                LoadSection(prefix, section);
                LoadData(prefix, section.GetChildren());
            }
        }

        private void LoadSection(string prefix, IConfigurationSection section)
        {
            if (section == null)
                return;
            if (string.IsNullOrEmpty(section.Value))
                return;
            Data[prefix + ConfigurationPath.KeyDelimiter + section.Path] = section.Value;
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

    }

    class JsonStreamConfigurationProvider : JsonConfigurationProvider
    {
        private JsonStreamConfigurationSource _source;
        internal JsonStreamConfigurationProvider(JsonStreamConfigurationSource source) : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            _source = source;
        }
        public override void Load()
        {
            base.Load(_source.Stream);
        }
    }

    class JsonStreamConfigurationSource : JsonConfigurationSource
    {
        internal MemoryStream Stream { get; }

        internal JsonStreamConfigurationSource(MemoryStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            Stream = stream;
        }
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonStreamConfigurationProvider(this);
        }
    }
}

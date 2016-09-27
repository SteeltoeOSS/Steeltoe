//
// Copyright 2015 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        private const string VCAP_PREFIX = "VCAP_";
        private const string APPLICATION = "APPLICATION";
        private const string SERVICES = "SERVICES";

        public CloudFoundryConfigurationProvider()
        {
        }
        public override void Load()
        {
            var builder = new ConfigurationBuilder();
            builder.AddEnvironmentVariables(VCAP_PREFIX);
            var vcap = builder.Build();
            Process(vcap);
        }

        private void Process(IConfigurationRoot vcap)
        {
            string appJson = vcap[APPLICATION];
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
                }
            }

            string appServicesJson = vcap[SERVICES];
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
            foreach(IConfigurationSection section in sections)
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

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
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
        MemoryStream _stream;
        internal JsonStreamConfigurationProvider(JsonConfigurationSource source, MemoryStream stream) : base(source)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            _stream = stream;
        }
        public override void Load()
        {
            base.Load(_stream);
        }
    }

    class JsonStreamConfigurationSource : JsonConfigurationSource
    {
        private MemoryStream _stream;

        internal JsonStreamConfigurationSource(MemoryStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            _stream = stream;
        }
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonStreamConfigurationProvider(this, _stream);
        }
    }
}

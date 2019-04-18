// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Env
{
    public class EnvEndpoint : AbstractEndpoint<EnvironmentDescriptor>
    {
        private readonly ILogger<EnvEndpoint> _logger;
        private readonly IConfiguration _configuration;
        private IHostingEnvironment _env;
        private Sanitizer _sanitizer;

        public EnvEndpoint(IEnvOptions options, IConfiguration configuration, IHostingEnvironment env, ILogger<EnvEndpoint> logger = null)
            : base(options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger;
            _sanitizer = new Sanitizer(options.KeysToSanitize);
        }

        public new IEnvOptions Options
        {
            get
            {
                return options as IEnvOptions;
            }
        }

        public override EnvironmentDescriptor Invoke()
        {
            return DoInvoke(_configuration);
        }

        public EnvironmentDescriptor DoInvoke(IConfiguration configuration)
        {
            IList<string> activeProfiles = new List<string>() { _env.EnvironmentName };
            IList<PropertySourceDescriptor> propertySources = GetPropertySources(configuration);
            return new EnvironmentDescriptor(activeProfiles, propertySources);
        }

        public virtual IList<PropertySourceDescriptor> GetPropertySources(IConfiguration configuration)
        {
            List<PropertySourceDescriptor> results = new List<PropertySourceDescriptor>();
            if (configuration is IConfigurationRoot root)
            {
                foreach (var provider in root.Providers)
                {
                    var psd = GetPropertySourceDescriptor(provider);
                    if (psd != null)
                    {
                        results.Add(psd);
                    }
                }
            }

            return results;
        }

        public virtual PropertySourceDescriptor GetPropertySourceDescriptor(IConfigurationProvider provider)
        {
            Dictionary<string, PropertyValueDescriptor> properties = new Dictionary<string, PropertyValueDescriptor>();
            var sourceName = GetPropertySourceName(provider);

            foreach (var key in GetFullKeyNames(provider, null, new HashSet<string>()).OrderBy(p => p))
            {
                if (provider.TryGet(key, out var value))
                {
                    var sanitized = _sanitizer.Sanitize(new KeyValuePair<string, string>(key, value));
                    properties.Add(sanitized.Key, new PropertyValueDescriptor(sanitized.Value));
                }
            }

            return new PropertySourceDescriptor(sourceName, properties);
        }

        public virtual string GetPropertySourceName(IConfigurationProvider provider)
        {
            return provider is FileConfigurationProvider fileProvider
                ? provider.GetType().Name + ": [" + fileProvider.Source.Path + "]"
                : provider.GetType().Name;
        }

        private HashSet<string> GetFullKeyNames(IConfigurationProvider provider, string rootKey, HashSet<string> initialKeys)
        {
            foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), rootKey))
            {
                string surrogateKey = key;
                if (rootKey != null)
                {
                    surrogateKey = rootKey + ":" + key;
                }

                GetFullKeyNames(provider, surrogateKey, initialKeys);

                if (!initialKeys.Any(k => k.StartsWith(surrogateKey)))
                {
                    initialKeys.Add(surrogateKey);
                }
            }

            return initialKeys;
        }
    }
}

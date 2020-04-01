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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public class ConfigServerConfigurationSource : IConfigurationSource
    {
        protected internal IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        protected internal IDictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Gets the default settings the Config Server client uses to contact the Config Server
        /// </summary>
        public ConfigServerClientSettings DefaultSettings { get; }

        /// <summary>
        /// Gets or sets gets the configuration the Config Server client uses to contact the Config Server.
        /// Values returned override the default values provided in <see cref="DefaultSettings"/>
        /// </summary>
        public IConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Gets the logger factory used by the Config Server client
        /// </summary>
        public ILoggerFactory LogFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource"/> class.
        /// </summary>
        /// <param name="configuration">configuration used by the Config Server client. Values will override those found in default settings</param>
        /// <param name="logFactory">optional logger factory used by the client</param>
        public ConfigServerConfigurationSource(IConfiguration configuration, ILoggerFactory logFactory = null)
             : this(new ConfigServerClientSettings(), configuration, logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource"/> class.
        /// </summary>
        /// <param name="defaultSettings">the default settings used by the Config Server client</param>
        /// <param name="configuration">configuration used by the Config Server client. Values will override those found in default settings</param>
        /// <param name="logFactory">optional logger factory used by the client</param>
        public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IConfiguration configuration, ILoggerFactory logFactory = null)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            DefaultSettings = defaultSettings ?? throw new ArgumentNullException(nameof(defaultSettings));
            LogFactory = logFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource"/> class.
        /// </summary>
        /// <param name="sources">configuration sources used by the Config Server client. The <see cref="Configuration"/> will be built from these sources and the
        /// values will override those found in <see cref="DefaultSettings"/></param>
        /// <param name="properties">properties to be used when sources are built</param>
        /// <param name="logFactory">optional logger factory used by the client</param>
        public ConfigServerConfigurationSource(IList<IConfigurationSource> sources, IDictionary<string, object> properties = null, ILoggerFactory logFactory = null)
            : this(new ConfigServerClientSettings(), sources, properties, logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationSource"/> class.
        /// </summary>
        /// <param name="defaultSettings">the default settings used by the Config Server client</param>
        /// <param name="sources">configuration sources used by the Config Server client. The <see cref="Configuration"/> will be built from these sources and the
        /// values will override those found in <see cref="DefaultSettings"/></param>
        /// <param name="properties">properties to be used when sources are built</param>
        /// <param name="logFactory">optional logger factory used by the client</param>
        public ConfigServerConfigurationSource(ConfigServerClientSettings defaultSettings, IList<IConfigurationSource> sources, IDictionary<string, object> properties = null, ILoggerFactory logFactory = null)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _sources = new List<IConfigurationSource>(sources);

            if (properties != null)
            {
                _properties = new Dictionary<string, object>(properties);
            }

            DefaultSettings = defaultSettings ?? throw new ArgumentNullException(nameof(defaultSettings));
            LogFactory = logFactory;
        }

        /// <summary>
        /// Returns a <see cref="ConfigServerConfigurationProvider"/> configured using the values from this <see cref="ConfigServerConfigurationSource"/>
        /// </summary>
        /// <param name="builder">not required</param>
        /// <returns>configuration provider</returns>
        public virtual IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            if (Configuration == null)
            {
                // Create our own builder to build sources
                ConfigurationBuilder configBuilder = new ConfigurationBuilder();
                foreach (IConfigurationSource s in _sources)
                {
                    configBuilder.Add(s);
                }

                // Use properties provided
                foreach (var p in _properties)
                {
                    configBuilder.Properties.Add(p);
                }

                // Create configuration
                Configuration = configBuilder.Build();
            }

            var certificateSource = _sources.FirstOrDefault(cSource => cSource is ICertificateSource);
            if (certificateSource != null && DefaultSettings.ClientCertificate == null)
            {
                var certConfigurer = Activator.CreateInstance((certificateSource as ICertificateSource).OptionsConfigurer, Configuration) as IConfigureNamedOptions<CertificateOptions>;
                var certOptions = new CertificateOptions();
                certConfigurer.Configure(certOptions);
                DefaultSettings.ClientCertificate = certOptions.Certificate;
            }

            return new ConfigServerConfigurationProvider(this);
        }
    }
}

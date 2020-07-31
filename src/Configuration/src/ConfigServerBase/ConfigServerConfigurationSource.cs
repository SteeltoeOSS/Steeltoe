// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
                var configBuilder = new ConfigurationBuilder();
                foreach (var s in _sources)
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

            return new ConfigServerConfigurationProvider(this);
        }
    }
}

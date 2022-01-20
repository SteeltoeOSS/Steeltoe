// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Placeholder
{
    /// <summary>
    /// Configuration provider that resolves placeholders
    /// A placeholder takes the form of <code> ${some:config:reference?default_if_not_present}></code>
    /// </summary>
    public class PlaceholderResolverProvider : IPlaceholderResolverProvider
    {
        internal IList<IConfigurationProvider> _providers = new List<IConfigurationProvider>();
        internal ILogger<PlaceholderResolverProvider> _logger;

        private IConfigurationRoot _configuration;

        /// <summary>
        /// Gets the configuration this placeholder resolver wraps
        /// </summary>
        public IConfiguration Configuration
        {
            get => _configuration ?? _originalConfiguration;
        }

        private IConfiguration _originalConfiguration;
        private readonly IList<string> _resolvedKeys = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderResolverProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration
        /// </summary>
        /// <param name="configuration">the configuration the provider uses when resolving placeholders</param>
        /// <param name="logFactory">the logger factory to use</param>
        public PlaceholderResolverProvider(IConfiguration configuration, ILoggerFactory logFactory = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _originalConfiguration = configuration;
            _logger = logFactory?.CreateLogger<PlaceholderResolverProvider>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderResolverProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration providers.  The <see cref="Configuration"/>
        /// will be created from these providers.
        /// </summary>
        /// <param name="providers">the configuration providers the resolver uses when resolving placeholders</param>
        /// <param name="logFactory">the logger factory to use</param>
        public PlaceholderResolverProvider(IList<IConfigurationProvider> providers, ILoggerFactory logFactory = null)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            _providers = providers;
            _logger = logFactory?.CreateLogger<PlaceholderResolverProvider>();
        }

        public IList<IConfigurationProvider> Providers
        {
            get { return _providers; }
        }

        public IList<string> ResolvedKeys => _resolvedKeys;

        /// <summary>
        /// Tries to get a configuration value for the specified key. If the value is a placeholder
        /// it will try to resolve the placeholder before returning it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        public bool TryGet(string key, out string value)
        {
            EnsureInitialized();
            var originalValue = Configuration[key];
            value = PropertyPlaceholderHelper.ResolvePlaceholders(originalValue, Configuration);

            if (value != originalValue && !ResolvedKeys.Contains(key))
            {
                ResolvedKeys.Add(key);
            }

            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Sets a configuration value for the specified key. No placeholder resolution is performed.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, string value)
        {
            EnsureInitialized();
            Configuration[key] = value;
        }

        /// <summary>
        /// Returns a change token if this provider supports change tracking, null otherwise.
        /// </summary>
        /// <returns>changed token</returns>
        public IChangeToken GetReloadToken()
        {
            EnsureInitialized();
            return Configuration.GetReloadToken();
        }

        /// <summary>
        /// Creates the <see cref="Configuration"/> from the providers if it has not done so already.
        /// If Configuration already exists, it will call Reload() on the underlying configuration
        /// </summary>
        public void Load()
        {
            EnsureInitialized();
            
            // placeholder provider maybe be used in one of two ways:
            // 1. it nests all existing providers that came before it and becomes the only provider registered at the application level (wrapped mode)
            // 2. it acts as last provider in a chain, and contributes overriding values to placeholder overrides (contributing mode)
            // This is mainly used with .NET 6 ConfigurationManager class that acts as both IConfigurationBuilder & IConfigurationRoot
            // When detected that it's used in contributing mode, a specialized configuration view is used that doesn't load providers (as that is handled by outer config manager)
            // we just need a IConfiguration that represents all providers that came before it and we're working under assumption that they are already loaded

            // wrapped view represents a slice of configuration providers that came prior to placeholder. if outer configuration providers are modified, it needs to be reconstructed
            if (_originalConfiguration is IConfigurationRoot root && _configuration is not null && (!root.Providers.SequenceEqual(_configuration.Providers) || _configuration.Providers.Contains(this)))
            {
                _configuration = new ConfigurationView(root.Providers.TakeWhile(x => !ReferenceEquals(x, this)).ToList());
            }
            _configuration?.Reload();
            
        }

        /// <summary>
        /// Returns the immediate descendant configuration keys for a given parent path based on this
        /// <see cref="Configuration"/>'s data and the set of keys returned by all the preceding providers.
        /// </summary>
        /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The child keys.</returns>
        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            EnsureInitialized();
            var section = parentPath == null ? Configuration : Configuration.GetSection(parentPath);
            var children = section.GetChildren();
            var keys = new List<string>();
            keys.AddRange(children.Select(c => c.Key));
            return keys.Concat(earlierKeys)
                .OrderBy(k => k, ConfigurationKeyComparer.Instance);
        }

        private void EnsureInitialized()
        {
            if (Configuration == null)
            {
                _configuration = new ConfigurationRoot(_providers);
            }
            
        }
    }
}

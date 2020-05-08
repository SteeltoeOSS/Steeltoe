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
using System;
using System.Linq;

namespace Steeltoe.Common.Configuration
{
    public static class ConfigurationValuesHelper
    {
        public static string GetSetting(string key, IConfiguration primary, IConfiguration secondary, IConfiguration resolve, string def)
        {
            // First check for key in primary
            var setting = GetString(key, primary, resolve, null);
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            // Next check for key in secondary
            setting = GetString(key, secondary, resolve, null);
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            return def;
        }

        /// <summary>
        /// Get setting from config searching the given configPrefix keys in order. Returns the first element with key.
        /// </summary>
        /// <param name="key">The key of the element to return.</param>
        /// <param name="config">IConfiguration to search through.</param>
        /// <param name="defaultValue">The default Value if no configuration is found.</param>
        /// <param name="configPrefixes">The prefixes to search for in given order.</param>
        /// <returns>Config value</returns>
        public static string GetSetting(string key, IConfiguration config, string defaultValue, params string[] configPrefixes)
        {
            foreach (var prefix in configPrefixes)
            {
                var section = config.GetSection(prefix);
                var result = section.GetValue<string>(key);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Get a setting from config by searching the given keys in order. Returns the first match.
        /// </summary>
        /// <param name="config">IConfiguration to search through.</param>
        /// <param name="defaultValue">The default Value if no configuration is found.</param>
        /// <param name="configKeys">The fully-qualified keys to search for in given order.</param>
        /// <returns>Value from config or default (if not found)</returns>
        public static string GetPreferredSetting(IConfiguration config, string defaultValue, params string[] configKeys)
        {
            foreach (var key in configKeys.Where(c => !string.IsNullOrEmpty(c)))
            {
                var result = config.GetValue<string>(key);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        public static int GetInt(string key, IConfiguration config, IConfiguration resolve, int def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val))
            {
                if (int.TryParse(val, out var result))
                {
                    return result;
                }
            }

            return def;
        }

        public static double GetDouble(string key, IConfiguration config, IConfiguration resolve, double def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val))
            {
                if (double.TryParse(val, out var result))
                {
                    return result;
                }
            }

            return def;
        }

        public static bool GetBoolean(string key, IConfiguration config, IConfiguration resolve, bool def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val))
            {
                if (bool.TryParse(val, out var result))
                {
                    return result;
                }
            }

            return def;
        }

        public static string GetString(string key, IConfiguration config, IConfiguration resolve, string def)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(nameof(key));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var val = config[key];
            if (!string.IsNullOrEmpty(val))
            {
                return PropertyPlaceholderHelper.ResolvePlaceholders(val, resolve);
            }

            return def;
        }
    }
}

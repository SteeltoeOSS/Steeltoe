// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

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

        public static int GetInt(string key, IConfiguration config, IConfiguration resolve, int def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out var result))
            {
                return result;
            }

            return def;
        }

        public static double GetDouble(string key, IConfiguration config, IConfiguration resolve, double def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val) && double.TryParse(val, out var result))
            {
                return result;
            }

            return def;
        }

        public static bool GetBoolean(string key, IConfiguration config, IConfiguration resolve, bool def)
        {
            var val = GetString(key, config, resolve, null);
            if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out var result))
            {
                return result;
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

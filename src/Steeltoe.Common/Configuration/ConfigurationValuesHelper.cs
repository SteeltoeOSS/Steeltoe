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
            if (!string.IsNullOrEmpty(val))
            {
                int result;
                if (int.TryParse(val, out result))
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
                double result;
                if (double.TryParse(val, out result))
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
                bool result;
                if (bool.TryParse(val, out result))
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

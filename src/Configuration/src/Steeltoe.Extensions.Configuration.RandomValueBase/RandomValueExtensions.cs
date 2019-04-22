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
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Configuration.RandomValue
{
    public static class RandomValueExtensions
    {
        /// <summary>
        /// Add a random value configuration source to the <see cref="ConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">the configuration builder</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var resolver = new RandomValueSource(loggerFactory);
            builder.Add(resolver);

            return builder;
        }

        /// <summary>
        /// Add a random value configuration source to the <see cref="ConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">the configuration builder</param>
        /// <param name="prefix">the prefix used for random key values, default 'random:'</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, string prefix, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentException(nameof(prefix));
            }

            if (!prefix.EndsWith(":"))
            {
                prefix = prefix + ":";
            }

            var resolver = new RandomValueSource(prefix, loggerFactory);
            builder.Add(resolver);

            return builder;
        }
    }
}

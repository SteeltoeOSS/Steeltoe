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
using System;

namespace Steeltoe.Common.Configuration
{
    public static class IConfigurationBuilderExtensions
    {
        /// <summary>
        /// Finds all placeholders of the form <code> ${some:config:reference?default_if_not_present}</code>,
        /// resolves them from other values in the configuration, adds resolved values to your configuration.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> which contains properties to be resolved</param>
        /// <param name="useEmptyStringIfNotFound">Replace unresolved placeholders with empty strings, so the application does not see them</param>
        /// <param name="logger">Optional logger</param>
        /// <returns><see cref="IConfigurationBuilder"/> with additional provider which contains resolved placeholders</returns>
        public static IConfigurationBuilder AddResolvedPlaceholders(this IConfigurationBuilder configurationBuilder, bool useEmptyStringIfNotFound = true, ILogger logger = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            return configurationBuilder.AddInMemoryCollection(PropertyPlaceholderHelper.GetResolvedConfigurationPlaceholders(configurationBuilder.Build(), logger, useEmptyStringIfNotFound));
        }
    }
}

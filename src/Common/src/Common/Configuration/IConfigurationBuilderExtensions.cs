// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Configuration.SpringBoot
{
    public static class SpringBootConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add a configuration source to the <see cref="ConfigurationBuilder"/> that reads SPRING_BOOT_APPLICATION from the environment and expands the child keys found within.
        /// Configuration keys in '.' delimited style are also converted to a format understood by .NET
        /// </summary>
        /// <param name="builder">the configuration builder</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddSpringBootEnv(this IConfigurationBuilder builder, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var source = new SpringBootEnvSource(loggerFactory);
            builder.Add(source);

            return builder;
        }
    }
}

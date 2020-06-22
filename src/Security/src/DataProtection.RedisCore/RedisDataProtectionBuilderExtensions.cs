﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Security.DataProtection.Redis;
using System;

namespace Steeltoe.Security.DataProtection
{
    public static class RedisDataProtectionBuilderExtensions
    {
        public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddSingleton<IXmlRepository, CloudFoundryRedisXmlRepository>();

            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>((p) =>
            {
                var config = new ConfigureNamedOptions<KeyManagementOptions>(Options.DefaultName, (options) =>
                {
                    options.XmlRepository = p.GetRequiredService<IXmlRepository>();
                });
                return config;
            });
            return builder;
        }
    }
}
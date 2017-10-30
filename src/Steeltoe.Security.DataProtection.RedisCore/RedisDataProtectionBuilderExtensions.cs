//
// Copyright 2015 the original author or authors.
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
//

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
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
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Steeltoe.Common
{
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// If an instance of <see cref="IApplicationInstanceInfo"/> is found, it is returned.
        /// Otherwise a default instance is returned.
        /// </summary>
        /// <param name="sp">Provider of services</param>
        /// <returns>An instance of <see cref="IApplicationInstanceInfo" /></returns>
        public static IApplicationInstanceInfo GetApplicationInstanceInfo(this IServiceProvider sp)
        {
            var appInfo = sp.GetService<IApplicationInstanceInfo>();
            if (appInfo == null)
            {
                var config = sp.GetRequiredService<IConfiguration>();
                appInfo = new ApplicationInstanceInfo(config, string.Empty);
            }

            return appInfo;
        }
    }
}

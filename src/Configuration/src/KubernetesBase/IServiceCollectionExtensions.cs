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
using Steeltoe.Common;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Removes any existing <see cref="IApplicationInstanceInfo"/> if found. Registers a <see cref="KubernetesApplicationOptions" />
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        public static IServiceCollection RegisterKubernetesApplicationInstanceInfo(this IServiceCollection serviceCollection)
        {
            var appInfo = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IApplicationInstanceInfo));
            if (appInfo != null)
            {
                serviceCollection.Remove(appInfo);
            }

            var sp = serviceCollection.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();

            var newAppInfo = new KubernetesApplicationOptions(config);
            serviceCollection.AddSingleton(typeof(IApplicationInstanceInfo), newAppInfo);

            return serviceCollection;
        }
    }
}

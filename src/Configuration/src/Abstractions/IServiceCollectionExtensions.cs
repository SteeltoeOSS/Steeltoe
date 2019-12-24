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
using System.Linq;

namespace Steeltoe.Extensions.Configuration
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// If an instance of <see cref="IServicesInfo"/> is found, it is returned.
        /// Otherwise a default instance is added to the collection and then returned.
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        /// <returns>Relevant <see cref="IServicesInfo" /></returns>
        public static IServicesInfo GetServicesInfo(this IServiceCollection serviceCollection)
        {
            var sp = serviceCollection.BuildServiceProvider();
            var servicesInfo = sp.GetServices<IServicesInfo>();
            if (!servicesInfo.Any())
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var newServicesInfo = new ServicesOptions(config);
                serviceCollection.AddSingleton(typeof(IServicesInfo), newServicesInfo);
            }

            return servicesInfo.First();
        }
    }
}

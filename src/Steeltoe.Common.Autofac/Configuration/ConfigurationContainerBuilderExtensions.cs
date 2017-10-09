//
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
//

using Autofac;
using Microsoft.Extensions.Configuration;
using System;


namespace Steeltoe.Common.Configuration.Autofac
{
    public static class ConfigurationContainerBuilderExtensions
    {
        public static void RegisterConfiguration(this ContainerBuilder container, IConfiguration configuration)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            container.RegisterInstance(configuration).As<IConfigurationRoot>().SingleInstance();
            container.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
        }
    }
}

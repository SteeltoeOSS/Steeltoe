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

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Steeltoe.Common.Options.Autofac
{
    public static class OptionsContainerBuilderExtensions
    {
        public static void RegisterOptions(this ContainerBuilder container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterGeneric(typeof(OptionsManager<>)).As(typeof(IOptions<>)).SingleInstance();
            container.RegisterGeneric(typeof(OptionsMonitor<>)).As(typeof(IOptionsMonitor<>)).SingleInstance();
            container.RegisterGeneric(typeof(OptionsCache<>)).As(typeof(IOptionsMonitorCache<>)).SingleInstance();
            container.RegisterGeneric(typeof(OptionsManager<>)).As(typeof(IOptionsSnapshot<>)).InstancePerRequest();
            container.RegisterGeneric(typeof(OptionsFactory<>)).As(typeof(IOptionsFactory<>)).InstancePerDependency();
        }

        public static void RegisterOption<TOption>(this ContainerBuilder container, IConfiguration config)
            where TOption : class
            => container.RegisterOption<TOption>(Microsoft.Extensions.Options.Options.DefaultName, config);

        public static void RegisterOption<TOption>(this ContainerBuilder container, string name, IConfiguration config)
            where TOption : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterInstance(new ConfigurationChangeTokenSource<TOption>(name, config)).As<IOptionsChangeTokenSource<TOption>>().SingleInstance();
            container.RegisterInstance(new NamedConfigureFromConfigurationOptions<TOption>(name, config)).As<IConfigureOptions<TOption>>().SingleInstance();
        }

        public static void RegisterPostConfigure<TOptions>(this ContainerBuilder container, Action<TOptions> configureOptions)
            where TOptions : class
            => container.RegisterPostConfigure(Microsoft.Extensions.Options.Options.DefaultName, configureOptions);

        public static void RegisterPostConfigure<TOptions>(this ContainerBuilder container, string name, Action<TOptions> configureOptions)
            where TOptions : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            container.RegisterInstance(new PostConfigureOptions<TOptions>(name, configureOptions)).As<IPostConfigureOptions<TOptions>>().SingleInstance();
        }
    }
}

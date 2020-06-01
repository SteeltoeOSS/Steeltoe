// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

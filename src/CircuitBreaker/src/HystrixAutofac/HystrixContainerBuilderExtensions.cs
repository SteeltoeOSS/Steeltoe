﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixContainerBuilderExtensions
    {
        public static void RegisterHystrixCommand<TService, TImplementation>(this ContainerBuilder container, IHystrixCommandGroupKey groupKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (groupKey == null)
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(TImplementation).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            container.RegisterType<TImplementation>().As<TService>().WithParameter(new TypedParameter(typeof(IHystrixCommandOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCommand<TService, TImplementation>(this ContainerBuilder container, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (groupKey == null)
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (commandKey == null)
            {
                throw new ArgumentNullException(nameof(commandKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            container.RegisterType<TImplementation>().As<TService>().WithParameter(new TypedParameter(typeof(IHystrixCommandOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCommand<TService>(this ContainerBuilder container, IHystrixCommandGroupKey groupKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (groupKey == null)
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(TService).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            container.RegisterType<TService>().WithParameter(new TypedParameter(typeof(IHystrixCommandOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCommand<TService>(this ContainerBuilder container, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (groupKey == null)
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (commandKey == null)
            {
                throw new ArgumentNullException(nameof(commandKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            container.RegisterType<TService>().WithParameter(new TypedParameter(typeof(IHystrixCommandOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCommand<TService>(this ContainerBuilder container, string groupKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCommand<TService>(container, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
        }

        public static void RegisterHystrixCommand<TService>(this ContainerBuilder container, string groupKey, string commandKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (string.IsNullOrEmpty(commandKey))
            {
                throw new ArgumentNullException(nameof(commandKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCommand<TService>(container, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey), config);
        }

        public static void RegisterHystrixCommand<TService, TImplementation>(this ContainerBuilder container, string groupKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCommand<TService, TImplementation>(container, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
        }

        public static void RegisterHystrixCommand<TService, TImplementation>(this ContainerBuilder container, string groupKey, string commandKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (string.IsNullOrEmpty(commandKey))
            {
                throw new ArgumentNullException(nameof(commandKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCommand<TService, TImplementation>(container, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey), config);
        }

        public static void RegisterHystrixCollapser<TService, TImplementation>(this ContainerBuilder container, IHystrixCollapserKey collapserKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (collapserKey == null)
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var opts = new HystrixCollapserOptions(collapserKey, null, dynOpts);

            container.RegisterType<TImplementation>().As<TService>().WithParameter(new TypedParameter(typeof(IHystrixCollapserOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCollapser<TService, TImplementation>(this ContainerBuilder container, IHystrixCollapserKey collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (collapserKey == null)
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var opts = new HystrixCollapserOptions(collapserKey, scope, null, dynOpts);
            container.RegisterType<TImplementation>().As<TService>().WithParameter(new TypedParameter(typeof(IHystrixCollapserOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCollapser<TService>(this ContainerBuilder container, IHystrixCollapserKey collapserKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (collapserKey == null)
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var opts = new HystrixCollapserOptions(collapserKey, null, dynOpts);
            container.RegisterType<TService>().WithParameter(new TypedParameter(typeof(IHystrixCollapserOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCollapser<TService>(this ContainerBuilder container, IHystrixCollapserKey collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (collapserKey == null)
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var opts = new HystrixCollapserOptions(collapserKey, scope, null, dynOpts);
            container.RegisterType<TService>().WithParameter(new TypedParameter(typeof(IHystrixCollapserOptions), opts)).InstancePerDependency();
        }

        public static void RegisterHystrixCollapser<TService>(this ContainerBuilder container, string collapserKey, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCollapser<TService>(container, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
        }

        public static void RegisterHystrixCollapser<TService>(this ContainerBuilder container, string collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCollapser<TService>(container, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
        }

        public static void RegisterHystrixCollapser<TService, TImplementation>(this ContainerBuilder container, string collapserKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCollapser<TService, TImplementation>(container, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
        }

        public static void RegisterHystrixCollapser<TService, TImplementation>(this ContainerBuilder container, string collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RegisterHystrixCollapser<TService, TImplementation>(container, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
        }
    }
}

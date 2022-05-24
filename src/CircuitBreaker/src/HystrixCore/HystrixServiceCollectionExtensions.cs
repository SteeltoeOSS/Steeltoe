// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {
        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey,
                ThreadPoolKey = threadPoolKey
            };

            opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
            services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }

        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey,
                ThreadPoolKey = threadPoolKey
            };
            opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
            services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }

        public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(TService).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey,
                ThreadPoolKey = threadPoolKey
            };
            opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
            services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }

        public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey,
                ThreadPoolKey = threadPoolKey
            };
            opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
            services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }

        public static void AddHystrixCommand<TService>(this IServiceCollection services, string groupKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCommand<TService>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
        }

        public static void AddHystrixCommand<TService>(this IServiceCollection services, string groupKey, string commandKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            AddHystrixCommand<TService>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey), config);
        }

        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, string groupKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(groupKey))
            {
                throw new ArgumentNullException(nameof(groupKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCommand<TService, TImplementation>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
        }

        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, string groupKey, string commandKey, IConfiguration config)
             where TService : class
             where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            AddHystrixCommand<TService, TImplementation>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey), config);
        }

        public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, IHystrixCollapserKey collapserKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }

        public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, IHystrixCollapserKey collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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
            services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }

        public static void AddHystrixCollapser<TService>(this IServiceCollection services, IHystrixCollapserKey collapserKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }

        public static void AddHystrixCollapser<TService>(this IServiceCollection services, IHystrixCollapserKey collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }

        public static void AddHystrixCollapser<TService>(this IServiceCollection services, string collapserKey, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCollapser<TService>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
        }

        public static void AddHystrixCollapser<TService>(this IServiceCollection services, string collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCollapser<TService>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
        }

        public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, string collapserKey, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCollapser<TService, TImplementation>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
        }

        public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, string collapserKey, RequestCollapserScope scope, IConfiguration config)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(collapserKey))
            {
                throw new ArgumentNullException(nameof(collapserKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddHystrixCollapser<TService, TImplementation>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
        }
    }
}

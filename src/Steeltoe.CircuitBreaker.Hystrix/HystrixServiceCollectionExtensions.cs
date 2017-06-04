using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {
        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config) 
            where TService : class where TImplementation : class, TService
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(TImplementation).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            services.AddTransient<TService, TImplementation>( (p) => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }
        public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config)
            where TService : class where TImplementation : class, TService
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };
            services.AddTransient<TService, TImplementation>((p) => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
        }
        public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config) where TService : class
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(TService).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };

            services.AddTransient<TService>((p) => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }
        public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config) where TService : class
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };

            services.AddTransient<TService>((p) => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
        }
    }
}

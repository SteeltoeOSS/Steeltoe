using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {

        public static void AddHystrixCommand<T>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config) where T : class
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            var commandKey = HystrixCommandKeyDefault.AsKey(typeof(T).Name);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };

            services.AddTransient<T>((p) => (T)ActivatorUtilities.CreateInstance(p, typeof(T), opts));
        }
        public static void AddHystrixCommand<T>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey, IConfiguration config) where T : class
        {
            var strategy = HystrixPlugins.OptionsStrategy;
            var dynOpts = strategy.GetDynamicOptions(config);

            IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
            {
                GroupKey = groupKey
            };

            services.AddTransient<T>((p) => (T)ActivatorUtilities.CreateInstance(p, typeof(T), opts));
        }
    }
}

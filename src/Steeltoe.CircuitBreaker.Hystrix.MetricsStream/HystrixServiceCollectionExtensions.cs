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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsStream;
using Steeltoe.CloudFoundry.Connector.Hystrix;


namespace Steeltoe.CircuitBreaker.Hystrix

{
    public static class HystrixServiceCollectionExtensions
    {
        public static void AddHystrixMetricsStream(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<HystrixDashboardStream>(HystrixDashboardStream.GetInstance());
            services.AddHystrixConnection(config);
            services.AddSingleton<HystrixMetricsStreamPublisher>();
        }

        public static void AddHystrixRequestEventStream(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<HystrixRequestEventsStream>(HystrixRequestEventsStream.GetInstance());
        }

        public static void AddHystrixUtilizationStream(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<HystrixUtilizationStream>(HystrixUtilizationStream.GetInstance());
        }

        public static void AddHystrixConfigStream(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<HystrixConfigurationStream>(HystrixConfigurationStream.GetInstance());
        }

        public static void AddHystrixMonitorStreams(this IServiceCollection services, IConfiguration config)
        {
            services.AddHystrixMetricsStream(config);
            services.AddHystrixConfigStream(config);
            services.AddHystrixRequestEventStream(config);
            services.AddHystrixUtilizationStream(config);
        }
    }
}

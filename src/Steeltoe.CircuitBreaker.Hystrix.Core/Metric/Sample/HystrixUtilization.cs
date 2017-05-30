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

using System.Collections.Generic;


namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixUtilization
    {
        private readonly Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationMap;
        private readonly Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationMap;

        public HystrixUtilization(Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationMap, 
            Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationMap)
        {
            this.commandUtilizationMap = commandUtilizationMap;
            this.threadPoolUtilizationMap = threadPoolUtilizationMap;
        }

        public static HystrixUtilization From(Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationMap,
                Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationMap)
        {
            return new HystrixUtilization(commandUtilizationMap, threadPoolUtilizationMap);
        }

        public Dictionary<IHystrixCommandKey, HystrixCommandUtilization> CommandUtilizationMap
        {
            get { return commandUtilizationMap; }
        }

        public Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> ThreadPoolUtilizationMap
        {
            get { return threadPoolUtilizationMap; }
        }
    }
}

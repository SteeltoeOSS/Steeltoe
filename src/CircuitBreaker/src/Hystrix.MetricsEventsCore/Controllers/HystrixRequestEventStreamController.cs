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

using Microsoft.AspNetCore.Mvc;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Serial;
using System;
using System.Reactive.Observable.Aliases;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers
{
    [Route("hystrix/request.stream")]
    public class HystrixRequestEventStreamController : HystrixStreamBaseController
    {
        public HystrixRequestEventStreamController(HystrixRequestEventsStream stream)
            : this(stream.Observe())
        {
        }

        private HystrixRequestEventStreamController(IObservable<HystrixRequestEvents> observable)
            : base(observable.Map((requestEvents) =>
            {
                return SerialHystrixRequestEvents.ToJsonString(requestEvents);
            }))
        {
        }

        [HttpGet]
        public async Task StartRequestEventStream()
        {
            HandleRequest();
            await Request.HttpContext.RequestAborted;
            SampleSubscription.Dispose();
        }
    }
}

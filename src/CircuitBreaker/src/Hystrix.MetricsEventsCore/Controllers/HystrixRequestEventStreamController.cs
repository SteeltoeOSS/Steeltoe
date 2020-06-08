// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

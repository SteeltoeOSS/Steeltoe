// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Serial;
using System;
using System.Reactive.Observable.Aliases;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers
{
    [Route("hystrix/config.stream")]
    public class HystrixConfigStreamController : HystrixStreamBaseController
    {
        public HystrixConfigStreamController(HystrixConfigurationStream stream)
            : this(stream.Observe())
        {
        }

        private HystrixConfigStreamController(IObservable<HystrixConfiguration> observable)
            : base(observable.Map((hystrixConfiguration) =>
            {
                return SerialHystrixConfiguration.ToJsonString(hystrixConfiguration);
            }))
        {
        }

        [HttpGet]
        public async Task StartConfigStream()
        {
            HandleRequest();
            await Request.HttpContext.RequestAborted;
            SampleSubscription.Dispose();
        }
    }
}

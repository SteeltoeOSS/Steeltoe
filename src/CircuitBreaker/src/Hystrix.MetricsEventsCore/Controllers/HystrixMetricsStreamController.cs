// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Serial;
using System;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers
{
    [Route("hystrix/hystrix.stream")]
    public class HystrixMetricsStreamController : HystrixStreamBaseController
    {
        public HystrixMetricsStreamController(HystrixDashboardStream stream)
            : this(stream.Observe())
        {
        }

        private HystrixMetricsStreamController(IObservable<HystrixDashboardStream.DashboardData> observable)
            : base(observable.Map((data) => SerialHystrixDashboardData.ToMultipleJsonStrings(data).ToObservable()).SelectMany(n => n))
        {
        }

        [HttpGet]
        public async Task StartMetricsStream()
        {
            HandleRequest();
            await Request.HttpContext.RequestAborted;
            SampleSubscription.Dispose();
        }
    }
}

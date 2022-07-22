// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.Serial;
using System;
using System.Reactive.Observable.Aliases;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers;

[Route("hystrix/utilization.stream")]
public class HystrixUtilizationStreamController : HystrixStreamBaseController
{
    public HystrixUtilizationStreamController(HystrixUtilizationStream stream)
        : this(stream.Observe())
    {
    }

    private HystrixUtilizationStreamController(IObservable<HystrixUtilization> observable)
        : base(observable.Map((utilization) =>
        {
            return SerialHystrixUtilization.ToJsonString(utilization);
        }))
    {
    }

    [HttpGet]
    public async Task StartUtilizationStream()
    {
        HandleRequest();
        await Request.HttpContext.RequestAborted;
        SampleSubscription.Dispose();
    }
}
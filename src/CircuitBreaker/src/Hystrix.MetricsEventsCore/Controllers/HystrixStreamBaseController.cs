// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers;

public class HystrixStreamBaseController : Controller
{
    protected internal IObservable<string> SampleStream { get; set; }

    protected internal IDisposable SampleSubscription { get; set; }

    public HystrixStreamBaseController(IObservable<string> observable)
    {
        SampleStream = observable;
    }

    protected void HandleRequest()
    {
        Response.StatusCode = 200;
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("Content-Type", "text/event-stream;charset=UTF-8");
        Response.Headers.Add("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");
        Response.Headers.Add("Pragma", "no-cache");

        SampleSubscription = SampleStream
            .ObserveOn(NewThreadScheduler.Default)
            .Subscribe(
                async sampleDataAsString =>
                {
                    if (sampleDataAsString != null)
                    {
                        try
                        {
                            await Response.WriteAsync($"data: {sampleDataAsString}\n\n").ConfigureAwait(false);
                            await Response.Body.FlushAsync().ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            if (SampleSubscription != null)
                            {
                                SampleSubscription.Dispose();
                                SampleSubscription = null;
                            }
                        }
                    }
                },
                error =>
                {
                    if (SampleSubscription != null)
                    {
                        SampleSubscription.Dispose();
                        SampleSubscription = null;
                    }
                },
                () =>
                {
                    if (SampleSubscription != null)
                    {
                        SampleSubscription.Dispose();
                        SampleSubscription = null;
                    }
                });
        Response.Body.FlushAsync();
    }
}

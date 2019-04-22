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

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers
{
    public class HystrixStreamBaseController : Controller
    {
        internal IObservable<string> sampleStream;

        internal IDisposable sampleSubscription = null;

        public HystrixStreamBaseController(IObservable<string> observable)
        {
            this.sampleStream = observable;
        }

        protected void HandleRequest()
        {
            Response.StatusCode = 200;
            Response.Headers.Add("Connection", "keep-alive");
            Response.Headers.Add("Content-Type", "text/event-stream;charset=UTF-8");
            Response.Headers.Add("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");

            sampleSubscription = sampleStream
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(
                    async (sampleDataAsString) =>
                    {
                        if (sampleDataAsString != null)
                        {
                            try
                            {
                                await Response.WriteAsync("data: " + sampleDataAsString + "\n\n");
                                await Response.Body.FlushAsync();
                            }
                            catch (Exception)
                            {
                                if (sampleSubscription != null)
                                {
                                    sampleSubscription.Dispose();
                                    sampleSubscription = null;
                                }
                            }
                        }
                    },
                    (error) =>
                    {
                        if (sampleSubscription != null)
                        {
                            sampleSubscription.Dispose();
                            sampleSubscription = null;
                        }
                    },
                    () =>
                    {
                        if (sampleSubscription != null)
                        {
                            sampleSubscription.Dispose();
                            sampleSubscription = null;
                        }
                    });
            Response.Body.FlushAsync();
        }
    }
}

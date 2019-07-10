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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers
{
    public class HystrixStreamBaseController : Controller
    {
        private IObservable<string> sampleStream;

        private IDisposable sampleSubscription = null;

        protected internal IObservable<string> SampleStream { get => sampleStream; set => sampleStream = value; }

        protected internal IDisposable SampleSubscription { get => sampleSubscription; set => sampleSubscription = value; }

        public HystrixStreamBaseController(IObservable<string> observable)
        {
            this.SampleStream = observable;
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
                    async (sampleDataAsString) =>
                    {
                        if (sampleDataAsString != null)
                        {
                            try
                            {
                                await Response.WriteAsync("data: " + sampleDataAsString + "\n\n").ConfigureAwait(false);
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
                    (error) =>
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
}

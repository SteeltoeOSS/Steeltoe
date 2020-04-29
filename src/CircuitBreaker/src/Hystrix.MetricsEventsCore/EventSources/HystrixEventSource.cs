using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources
{
    public class HystrixEventSource : EventSource
    {
        [EventIgnore]
        protected internal IObservable<string> SampleStream { get; set; }
        [EventIgnore]
        protected internal IDisposable SampleSubscription { get; set; } = null;

        protected HystrixEventSource(IObservable<string> observable)
        {
            this.SampleStream = observable;

            SampleSubscription = SampleStream
                .ObserveOn(NewThreadScheduler.Default)
                    .Subscribe(
                        async (sampleDataAsString) =>
                        {
                            if (sampleDataAsString != null)
                            {
                                try
                                {
                                    //await Response.WriteAsync("data: " + sampleDataAsString + "\n\n").ConfigureAwait(false);
                                    //await Response.Body.FlushAsync().ConfigureAwait(false);
                                    LogEvent(sampleDataAsString);
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
        }

        [Event(1)]
        public void LogEvent(string data)
        {
            WriteEvent(1, data);
        }
    }
}

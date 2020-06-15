// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker
{
    internal class HystrixCircuitBreakerImpl : ICircuitBreaker
    {
        private readonly IHystrixCommandOptions options;
        private readonly HystrixCommandMetrics metrics;

        /* track whether this circuit is open/closed at any given point in time (default to false==closed) */
        private AtomicBoolean circuitOpen = new AtomicBoolean(false);

        /* when the circuit was marked open or was last allowed to try a 'singleTest' */
        private AtomicLong circuitOpenedOrLastTestedTime = new AtomicLong();

        protected internal HystrixCircuitBreakerImpl(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixCommandOptions options, HystrixCommandMetrics metrics)
        {
            this.options = options;
            this.metrics = metrics;
        }

        public virtual void MarkSuccess()
        {
            if (circuitOpen.Value && circuitOpen.CompareAndSet(true, false))
            {
                // win the thread race to reset metrics
                // Unsubscribe from the current stream to reset the health counts stream.  This only affects the health counts view,
                // and all other metric consumers are unaffected by the reset
                metrics.ResetStream();
            }
        }

        public bool AllowRequest
        {
            get
            {
                if (options.CircuitBreakerForceOpen)
                {
                    // properties have asked us to force the circuit open so we will allow NO requests
                    return false;
                }

                if (options.CircuitBreakerForceClosed)
                {
                    // we still want to allow isOpen() to perform it's calculations so we simulate normal behavior
                    var isOpen = IsOpen;

                    // properties have asked us to ignore errors so we will ignore the results of isOpen and just allow all traffic through
                    return true;
                }

                return !IsOpen || AllowSingleTest();
            }
        }

        public virtual bool AllowSingleTest()
        {
            long timeCircuitOpenedOrWasLastTested = circuitOpenedOrLastTestedTime.Value;

            // 1) if the circuit is open
            // 2) and it's been longer than 'sleepWindow' since we opened the circuit
            if (circuitOpen.Value && Time.CurrentTimeMillis > timeCircuitOpenedOrWasLastTested + options.CircuitBreakerSleepWindowInMilliseconds)
            {
                // We push the 'circuitOpenedTime' ahead by 'sleepWindow' since we have allowed one request to try.
                // If it succeeds the circuit will be closed, otherwise another singleTest will be allowed at the end of the 'sleepWindow'.
#pragma warning disable S1066 // Collapsible "if" statements should be merged
                if (circuitOpenedOrLastTestedTime.CompareAndSet(timeCircuitOpenedOrWasLastTested, Time.CurrentTimeMillis))
#pragma warning restore S1066 // Collapsible "if" statements should be merged
                {
                    // if this returns true that means we set the time so we'll return true to allow the singleTest
                    // if it returned false it means another thread raced us and allowed the singleTest before we did
                    return true;
                }
            }

            return false;
        }

        public bool IsOpen
        {
            get
            {
                if (circuitOpen.Value)
                {
                    // if we're open we immediately return true and don't bother attempting to 'close' ourself as that is left to allowSingleTest and a subsequent successful test to close
                    return true;
                }

                // we're closed, so let's see if errors have made us so we should trip the circuit open
                HealthCounts health = metrics.Healthcounts;

                // check if we are past the statisticalWindowVolumeThreshold
                if (health.TotalRequests < options.CircuitBreakerRequestVolumeThreshold)
                {
                    // we are not past the minimum volume threshold for the statisticalWindow so we'll return false immediately and not calculate anything
                    return false;
                }

                if (health.ErrorPercentage < options.CircuitBreakerErrorThresholdPercentage)
                {
                    return false;
                }
                else
                {
                    // our failure rate is too high, trip the circuit
                    if (circuitOpen.CompareAndSet(false, true))
                    {
                        // if the previousValue was false then we want to set the currentTime
                        circuitOpenedOrLastTestedTime.Value = Time.CurrentTimeMillis;
                        return true;
                    }
                    else
                    {
                        // How could previousValue be true? If another thread was going through this code at the same time a race-condition could have
                        // caused another thread to set it to true already even though we were in the process of doing the same
                        // In this case, we know the circuit is open, so let the other thread set the currentTime and report back that the circuit is open
                        return true;
                    }
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class CachedValuesHistogram
    {
        private const int NUMBER_SIGNIFICANT_DIGITS = 3;

        private readonly int _mean;
        private readonly int _p0;
        private readonly int _p5;
        private readonly int _p10;
        private readonly int _p15;
        private readonly int _p20;
        private readonly int _p25;
        private readonly int _p30;
        private readonly int _p35;
        private readonly int _p40;
        private readonly int _p45;
        private readonly int _p50;
        private readonly int _p55;
        private readonly int _p60;
        private readonly int _p65;
        private readonly int _p70;
        private readonly int _p75;
        private readonly int _p80;
        private readonly int _p85;
        private readonly int _p90;
        private readonly int _p95;
        private readonly int _p99;
        private readonly int _p99_5;
        private readonly int _p99_9;
        private readonly int _p99_95;
        private readonly int _p99_99;
        private readonly int _p100;

        private readonly long _totalCount;

        public static LongHistogram GetNewHistogram()
        {
            LongHistogram histo = new LongHistogram(1, 2, NUMBER_SIGNIFICANT_DIGITS);
            histo.Reset();
            return histo;
        }

        public static CachedValuesHistogram BackedBy(LongHistogram underlying)
        {
            return new CachedValuesHistogram(underlying);
        }

        private CachedValuesHistogram(LongHistogram underlying)
        {
            /*
             * Single thread calculates a variety of commonly-accessed quantities.
             * This way, all threads can access the cached values without synchronization
             * Synchronization is only required for values that are not cached
             */
            if (underlying.TotalCount > 0)
            {
                _mean = (int)underlying.GetMean();
                _p0 = (int)underlying.GetValueAtPercentile(0);
                _p5 = (int)underlying.GetValueAtPercentile(5);
                _p10 = (int)underlying.GetValueAtPercentile(10);
                _p15 = (int)underlying.GetValueAtPercentile(15);
                _p20 = (int)underlying.GetValueAtPercentile(20);
                _p25 = (int)underlying.GetValueAtPercentile(25);
                _p30 = (int)underlying.GetValueAtPercentile(30);
                _p35 = (int)underlying.GetValueAtPercentile(35);
                _p40 = (int)underlying.GetValueAtPercentile(40);
                _p45 = (int)underlying.GetValueAtPercentile(45);
                _p50 = (int)underlying.GetValueAtPercentile(50);
                _p55 = (int)underlying.GetValueAtPercentile(55);
                _p60 = (int)underlying.GetValueAtPercentile(60);
                _p65 = (int)underlying.GetValueAtPercentile(65);
                _p70 = (int)underlying.GetValueAtPercentile(70);
                _p75 = (int)underlying.GetValueAtPercentile(75);
                _p80 = (int)underlying.GetValueAtPercentile(80);
                _p85 = (int)underlying.GetValueAtPercentile(85);
                _p90 = (int)underlying.GetValueAtPercentile(90);
                _p95 = (int)underlying.GetValueAtPercentile(95);
                _p99 = (int)underlying.GetValueAtPercentile(99);
                _p99_5 = (int)underlying.GetValueAtPercentile(99.5);
                _p99_9 = (int)underlying.GetValueAtPercentile(99.9);
                _p99_95 = (int)underlying.GetValueAtPercentile(99.95);
                _p99_99 = (int)underlying.GetValueAtPercentile(99.99);
                _p100 = (int)underlying.GetValueAtPercentile(100);

                _totalCount = underlying.TotalCount;
            }
        }

         // Return the cached value only
        public int GetMean()
        {
            return _mean;
        }

         // Return the cached value if available. Otherwise, we need to synchronize access to the underlying {@link Histogram}
        public int GetValueAtPercentile(double percentile)
        {
            int permyriad = (int)percentile * 100;
            switch (permyriad)
            {
                case 0: return _p0;
                case 500: return _p5;
                case 1000: return _p10;
                case 1500: return _p15;
                case 2000: return _p20;
                case 2500: return _p25;
                case 3000: return _p30;
                case 3500: return _p35;
                case 4000: return _p40;
                case 4500: return _p45;
                case 5000: return _p50;
                case 5500: return _p55;
                case 6000: return _p60;
                case 6500: return _p65;
                case 7000: return _p70;
                case 7500: return _p75;
                case 8000: return _p80;
                case 8500: return _p85;
                case 9000: return _p90;
                case 9500: return _p95;
                case 9900: return _p99;
                case 9950: return _p99_5;
                case 9990: return _p99_9;
                case 9995: return _p99_95;
                case 9999: return _p99_99;
                case 10000: return _p100;
                default: throw new ArgumentException("Percentile (" + percentile + ") is not currently cached");
            }
        }

        public long GetTotalCount()
        {
            return _totalCount;
        }

        public override string ToString()
        {
            return "[Mean: " + GetMean() + "/Total: " + GetTotalCount() + "]";
        }
    }
}

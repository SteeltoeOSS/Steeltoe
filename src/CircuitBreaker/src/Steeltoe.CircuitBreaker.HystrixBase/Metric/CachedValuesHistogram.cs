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

using HdrHistogram;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class CachedValuesHistogram
    {
        private const int NUMBER_SIGNIFICANT_DIGITS = 3;

        private readonly int mean;
        private readonly int p0;
        private readonly int p5;
        private readonly int p10;
        private readonly int p15;
        private readonly int p20;
        private readonly int p25;
        private readonly int p30;
        private readonly int p35;
        private readonly int p40;
        private readonly int p45;
        private readonly int p50;
        private readonly int p55;
        private readonly int p60;
        private readonly int p65;
        private readonly int p70;
        private readonly int p75;
        private readonly int p80;
        private readonly int p85;
        private readonly int p90;
        private readonly int p95;
        private readonly int p99;
        private readonly int p99_5;
        private readonly int p99_9;
        private readonly int p99_95;
        private readonly int p99_99;
        private readonly int p100;

        private readonly long totalCount;

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
            /**
             * Single thread calculates a variety of commonly-accessed quantities.
             * This way, all threads can access the cached values without synchronization
             * Synchronization is only required for values that are not cached
             */
            if (underlying.TotalCount > 0)
            {
                mean = (int)underlying.GetMean();
                p0 = (int)underlying.GetValueAtPercentile(0);
                p5 = (int)underlying.GetValueAtPercentile(5);
                p10 = (int)underlying.GetValueAtPercentile(10);
                p15 = (int)underlying.GetValueAtPercentile(15);
                p20 = (int)underlying.GetValueAtPercentile(20);
                p25 = (int)underlying.GetValueAtPercentile(25);
                p30 = (int)underlying.GetValueAtPercentile(30);
                p35 = (int)underlying.GetValueAtPercentile(35);
                p40 = (int)underlying.GetValueAtPercentile(40);
                p45 = (int)underlying.GetValueAtPercentile(45);
                p50 = (int)underlying.GetValueAtPercentile(50);
                p55 = (int)underlying.GetValueAtPercentile(55);
                p60 = (int)underlying.GetValueAtPercentile(60);
                p65 = (int)underlying.GetValueAtPercentile(65);
                p70 = (int)underlying.GetValueAtPercentile(70);
                p75 = (int)underlying.GetValueAtPercentile(75);
                p80 = (int)underlying.GetValueAtPercentile(80);
                p85 = (int)underlying.GetValueAtPercentile(85);
                p90 = (int)underlying.GetValueAtPercentile(90);
                p95 = (int)underlying.GetValueAtPercentile(95);
                p99 = (int)underlying.GetValueAtPercentile(99);
                p99_5 = (int)underlying.GetValueAtPercentile(99.5);
                p99_9 = (int)underlying.GetValueAtPercentile(99.9);
                p99_95 = (int)underlying.GetValueAtPercentile(99.95);
                p99_99 = (int)underlying.GetValueAtPercentile(99.99);
                p100 = (int)underlying.GetValueAtPercentile(100);

                totalCount = underlying.TotalCount;
            }
        }

         // Return the cached value only
        public int GetMean()
        {
            return mean;
        }

         // Return the cached value if available. Otherwise, we need to synchronize access to the underlying {@link Histogram}
        public int GetValueAtPercentile(double percentile)
        {
            int permyriad = (int)percentile * 100;
            switch (permyriad)
            {
                case 0: return p0;
                case 500: return p5;
                case 1000: return p10;
                case 1500: return p15;
                case 2000: return p20;
                case 2500: return p25;
                case 3000: return p30;
                case 3500: return p35;
                case 4000: return p40;
                case 4500: return p45;
                case 5000: return p50;
                case 5500: return p55;
                case 6000: return p60;
                case 6500: return p65;
                case 7000: return p70;
                case 7500: return p75;
                case 8000: return p80;
                case 8500: return p85;
                case 9000: return p90;
                case 9500: return p95;
                case 9900: return p99;
                case 9950: return p99_5;
                case 9990: return p99_9;
                case 9995: return p99_95;
                case 9999: return p99_99;
                case 10000: return p100;
                default: throw new ArgumentException("Percentile (" + percentile + ") is not currently cached");
            }
        }

        public long GetTotalCount()
        {
            return totalCount;
        }
    }
}

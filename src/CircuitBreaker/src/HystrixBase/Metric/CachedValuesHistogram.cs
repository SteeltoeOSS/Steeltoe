// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

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
        var histo = new LongHistogram(1, 2, NUMBER_SIGNIFICANT_DIGITS);
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
        var permyriad = (int)percentile * 100;
        return permyriad switch
        {
            0 => _p0,
            500 => _p5,
            1000 => _p10,
            1500 => _p15,
            2000 => _p20,
            2500 => _p25,
            3000 => _p30,
            3500 => _p35,
            4000 => _p40,
            4500 => _p45,
            5000 => _p50,
            5500 => _p55,
            6000 => _p60,
            6500 => _p65,
            7000 => _p70,
            7500 => _p75,
            8000 => _p80,
            8500 => _p85,
            9000 => _p90,
            9500 => _p95,
            9900 => _p99,
            9950 => _p99_5,
            9990 => _p99_9,
            9995 => _p99_95,
            9999 => _p99_99,
            10000 => _p100,
            _ => throw new ArgumentException($"Percentile ({percentile}) is not currently cached"),
        };
    }

    public long GetTotalCount()
    {
        return _totalCount;
    }

    public override string ToString()
    {
        return $"[Mean: {GetMean()}/Total: {GetTotalCount()}]";
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test;

public class MetricSampleTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var sample = new MetricSample(MetricStatistic.TOTAL, 100.00);
        Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);
        Assert.Equal(100.00, sample.Value);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var sample = new MetricSample(MetricStatistic.TOTAL, 100.00);
        var result = Serialize(sample);
        Assert.Equal("{\"statistic\":\"TOTAL\",\"value\":100}", result);
    }
}
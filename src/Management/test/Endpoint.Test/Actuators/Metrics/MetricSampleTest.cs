// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricSampleTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var sample = new MetricSample(MetricStatistic.Total, 100.00, null);
        Assert.Equal(MetricStatistic.Total, sample.Statistic);
        Assert.Equal(100.00, sample.Value);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var sample = new MetricSample(MetricStatistic.Total, 100.00, null);
        string result = Serialize(sample);
        Assert.Equal("{\"statistic\":\"TOTAL\",\"value\":100}", result);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricsResponseTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        List<MetricSample> samples = [new MetricSample(MetricStatistic.TotalTime, 100.00, null)];

        List<MetricTag> tags =
        [
            new MetricTag("tag", new HashSet<string>
            {
                "tagValue"
            })
        ];

        var response = new MetricsResponse("foo.bar", samples, tags);
        Assert.Equal("foo.bar", response.Name);
        Assert.Same(samples, response.Measurements);
        Assert.Same(tags, response.AvailableTags);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        List<MetricSample> samples = [new MetricSample(MetricStatistic.TotalTime, 100.1, null)];

        List<MetricTag> tags =
        [
            new MetricTag("tag", new HashSet<string>
            {
                "tagValue"
            })
        ];

        var response = new MetricsResponse("foo.bar", samples, tags);
        string result = Serialize(response);

        result.Should().BeJson("""
            {
              "name": "foo.bar",
              "measurements": [
                {
                  "statistic": "TOTAL_TIME",
                  "value": 100.1
                }
              ],
              "availableTags": [
                {
                  "tag": "tag",
                  "values": [
                    "tagValue"
                  ]
                }
              ]
            }
            """);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.MetricCollectors.Metrics;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public sealed class MetricsEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public MetricsEndpointTest(ITestOutputHelper output)
    {
        _output = output;
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();
    }

    [Fact]
    public async Task Invoke_WithNullMetricsRequest_ReturnsExpected()
    {
        using (var testContext = new TestContext(_output))
        {
            testContext.AdditionalServices = (services, _) =>
            {
                services.AddMetricsActuatorServices();
            };

            MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

            await service.StartAsync(CancellationToken.None);

            try
            {
                var handler = testContext.GetRequiredService<IMetricsEndpointHandler>();
                Counter<long> requests = SteeltoeMetrics.Meter.CreateCounter<long>("http.server.requests");
                requests.Add(1);
                Counter<double> memory = SteeltoeMetrics.Meter.CreateCounter<double>("gc.memory.used");
                memory.Add(25);

                MetricsResponse result = await handler.InvokeAsync(null, CancellationToken.None);
                Assert.NotNull(result);
                Assert.NotEmpty(result.Names);
                Assert.Contains("http.server.requests", result.Names);
                Assert.Contains("gc.memory.used", result.Names);

                Assert.Equal(2, result.Names.Count);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        using (var testContext = new TestContext(_output))
        {
            testContext.AdditionalServices = (services, _) =>
            {
                services.AddMetricsActuatorServices();
            };

            MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

            await service.StartAsync(CancellationToken.None);

            try
            {
                var handler = testContext.GetRequiredService<IMetricsEndpointHandler>();
                MetricsResponse result = await handler.InvokeAsync(null, CancellationToken.None);
                Assert.NotNull(result);

                Assert.IsType<MetricsResponse>(result);
                MetricsResponse response = result;
                Assert.Empty(response.Names);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithMetricsRequest_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = testContext.GetRequiredService<IMetricsEndpointHandler>();

            Counter<double> testMeasure = SteeltoeMetrics.Meter.CreateCounter<double>("test.test5");
            long allKeysSum = 0;

            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            for (int index = 0; index < 10; index++)
            {
                allKeysSum += index;
                testMeasure.Add(index, labels.AsReadonlySpan());
            }

            List<KeyValuePair<string, string>> tags = labels.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString())).ToList();
            var request = new MetricsRequest("test.test5", tags);
            MetricsResponse response = await handler.InvokeAsync(request, CancellationToken.None);
            Assert.NotNull(response);

            Assert.Equal("test.test5", response.Name);

            Assert.NotNull(response.Measurements);
            Assert.Single(response.Measurements);

            MetricSample sample = response.Measurements.SingleOrDefault(metricSample => metricSample.Statistic == MetricStatistic.Rate);
            Assert.NotNull(sample);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(response.AvailableTags);
            Assert.Equal(3, response.AvailableTags.Count);

            request = new MetricsRequest("foo.bar", tags);
            response = await handler.InvokeAsync(request, CancellationToken.None);
            Assert.Null(response);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Invoke_WithMetricsRequest_ReturnsExpected_IncludesAdditionalInstruments()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["management:endpoints:metrics:includedmetrics:0"] = "AdditionalTestMeter:AdditionalInstrument"
            });
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuator();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = testContext.GetRequiredService<IMetricsEndpointHandler>();

            Counter<double> testMeasure = SteeltoeMetrics.Meter.CreateCounter<double>("test.test5");
            var additionalMeter = new Meter("AdditionalTestMeter");
            Counter<double> additionalInstrument = additionalMeter.CreateCounter<double>("AdditionalInstrument");

            long allKeysSum = 0;

            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            for (int index = 0; index < 10; index++)
            {
                allKeysSum += index;
                testMeasure.Add(index, labels.AsReadonlySpan());
                additionalInstrument.Add(index, labels.AsReadonlySpan());
            }

            List<KeyValuePair<string, string>> tags = labels.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString())).ToList();
            var request = new MetricsRequest("test.test5", tags);
            MetricsResponse response = await handler.InvokeAsync(request, CancellationToken.None);
            Assert.NotNull(response);

            Assert.Equal("test.test5", response.Name);

            Assert.NotNull(response.Measurements);
            Assert.Single(response.Measurements);

            MetricSample sample = response.Measurements.SingleOrDefault(metricSample => metricSample.Statistic == MetricStatistic.Rate);
            Assert.NotNull(sample);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(response.AvailableTags);
            Assert.Equal(3, response.AvailableTags.Count);

            request = new MetricsRequest("AdditionalInstrument", tags);
            response = await handler.InvokeAsync(request, CancellationToken.None);
            Assert.NotNull(response);

            Assert.Equal("AdditionalInstrument", response.Name);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetMetricSamples_ReturnsExpectedCounter()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = (MetricsEndpointHandler)testContext.GetRequiredService<IMetricsEndpointHandler>();

            Counter<double> counter = SteeltoeMetrics.Meter.CreateCounter<double>("test.test7");
            counter.Add(100);

            (MetricsCollection<List<MetricSample>> measurements, _) = handler.GetMetrics();
            Assert.NotNull(measurements);
            Assert.Single(measurements.Values);
            MetricSample sample = measurements.Values.First()[0];
            Assert.Equal(100, sample.Value);
            Assert.Equal(MetricStatistic.Rate, sample.Statistic);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetAvailableTags_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = (MetricsEndpointHandler)testContext.GetRequiredService<IMetricsEndpointHandler>();
            Counter<double> counter = SteeltoeMetrics.Meter.CreateCounter<double>("test.test2");

            var v1Tags = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            var v2Tags = new Dictionary<string, object>
            {
                { "a", "v2" },
                { "b", "v2" },
                { "c", "v2" }
            };

            counter.Add(1, v1Tags.AsReadonlySpan());
            counter.Add(1, v2Tags.AsReadonlySpan());

            (_, MetricsCollection<List<MetricTag>> tagDictionary) = handler.GetMetrics();

            Assert.NotNull(tagDictionary);
            Assert.Single(tagDictionary.Values);

            List<MetricTag> tags = tagDictionary["test.test2"];

            Assert.Equal(3, tags.Count);

            MetricTag tag = tags[0];
            Assert.NotNull(tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            tag = tags[1];
            Assert.Equal("b", tag.Tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            tag = tags[2];
            Assert.Equal("c", tag.Tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            Counter<double> counter2 = SteeltoeMetrics.Meter.CreateCounter<double>("test.test3");

            counter2.Add(1);

            (_, tagDictionary) = handler.GetMetrics();

            Assert.NotNull(tagDictionary);
            Assert.Single(tagDictionary.Values);

            tags = tagDictionary["test.test3"];
            Assert.Empty(tags);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetMetricMeasurements_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = (MetricsEndpointHandler)testContext.GetRequiredService<IMetricsEndpointHandler>();

            Histogram<double> testMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("test.test1");

            var context1 = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            var context2 = new Dictionary<string, object>
            {
                { "a", "v1" }
            };

            var context3 = new Dictionary<string, object>
            {
                { "b", "v1" }
            };

            var context4 = new Dictionary<string, object>
            {
                { "c", "v1" }
            };

            long allKeysSum = 0;

            for (int index = 0; index < 10; index++)
            {
                allKeysSum += index;
                testMeasure.Record(index, context1.AsReadonlySpan());
            }

            long aSum = 0;

            for (int index = 0; index < 10; index++)
            {
                aSum += index;
                testMeasure.Record(index, context2.AsReadonlySpan());
            }

            long bSum = 0;

            for (int index = 0; index < 10; index++)
            {
                bSum += index;
                testMeasure.Record(index, context3.AsReadonlySpan());
            }

            long cSum = 0;

            for (int index = 0; index < 10; index++)
            {
                cSum += index;
                testMeasure.Record(index, context4.AsReadonlySpan());
            }

            (MetricsCollection<List<MetricSample>> measurements, _) = handler.GetMetrics();
            Assert.NotNull(measurements);
            Assert.Single(measurements);

            List<MetricSample> measurement = measurements["test.test1"];
            Assert.Equal(4, measurement.Count);

            MetricSample sample = measurement[0];
            Assert.Equal(allKeysSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var aTags = new List<KeyValuePair<string, string>>
            {
                new("a", "v1")
            };

            IList<MetricSample> result = handler.GetMetricSamplesByTags(measurements, "test.test1", aTags);
            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];
            Assert.Equal(allKeysSum + aSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var bTags = new List<KeyValuePair<string, string>>
            {
                new("b", "v1")
            };

            result = handler.GetMetricSamplesByTags(measurements, "test.test1", bTags);

            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];

            Assert.Equal(allKeysSum + bSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var cTags = new List<KeyValuePair<string, string>>
            {
                new("c", "v1")
            };

            result = handler.GetMetricSamplesByTags(measurements, "test.test1", cTags);
            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];
            Assert.Equal(allKeysSum + cSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var abTags = new List<KeyValuePair<string, string>>
            {
                new("a", "v1"),
                new("b", "v1")
            };

            result = handler.GetMetricSamplesByTags(measurements, "test.test1", abTags);

            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];
            Assert.Equal(allKeysSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var acTags = new List<KeyValuePair<string, string>>
            {
                new("a", "v1"),
                new("c", "v1")
            };

            result = handler.GetMetricSamplesByTags(measurements, "test.test1", acTags);

            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];

            Assert.Equal(allKeysSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var bcTags = new List<KeyValuePair<string, string>>
            {
                new("b", "v1"),
                new("c", "v1")
            };

            result = handler.GetMetricSamplesByTags(measurements, "test.test1", bcTags);

            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];

            Assert.Equal(allKeysSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetMetric_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = testContext.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().First();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var handler = testContext.GetRequiredService<IMetricsEndpointHandler>();

            Counter<double> testMeasure = SteeltoeMetrics.Meter.CreateCounter<double>("test.total");

            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            double allKeysSum = 0;

            for (double index = 0; index < 10; index++)
            {
                allKeysSum += index;
                testMeasure.Add(index, labels.AsReadonlySpan());
            }

            var request = new MetricsRequest("test.total", labels.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString())).ToList());

            MetricsResponse response = await handler.InvokeAsync(request, CancellationToken.None);

            Assert.NotNull(response);

            Assert.Equal("test.total", response.Name);

            Assert.NotNull(response.Measurements);
            Assert.Single(response.Measurements);
            MetricSample sample = response.Measurements[0];
            Assert.Equal(MetricStatistic.Rate, sample.Statistic);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(response.AvailableTags);
            Assert.Equal(3, response.AvailableTags.Count);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }
}

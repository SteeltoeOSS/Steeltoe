// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;
using Steeltoe.Management.MetricCollectors.Metrics;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class MetricsEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public MetricsEndpointTest(ITestOutputHelper output)
    {
        _output = output;
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();
    }

    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new MetricsEndpoint(null, null, null));
        IOptionsMonitor<MetricsEndpointOptions> options = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        Assert.Throws<ArgumentNullException>(() => new MetricsEndpoint(options, null, null));
        Assert.Throws<ArgumentNullException>(() => new MetricsEndpoint(options, new SteeltoeExporter(null), null));
    }

    [Fact]
    public async Task Invoke_WithNullMetricsRequest_ReturnsExpected()
    {
        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddMetricsActuatorServices();
            };

            MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

            await service.StartAsync(CancellationToken.None);

            try
            {
                var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;
                Counter<long> requests = SteeltoeMetrics.Meter.CreateCounter<long>("http.server.requests");
                requests.Add(1);
                Counter<double> memory = SteeltoeMetrics.Meter.CreateCounter<double>("gc.memory.used");
                memory.Add(25);

                IMetricsResponse result = ep.Invoke(null);
                Assert.NotNull(result);
                Assert.IsType<MetricsListNamesResponse>(result);
                var resp = result as MetricsListNamesResponse;
                Assert.NotEmpty(resp.Names);
                Assert.Contains("http.server.requests", resp.Names);
                Assert.Contains("gc.memory.used", resp.Names);

                Assert.Equal(2, resp.Names.Count);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        using (var tc = new TestContext(_output))
        {
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddMetricsActuatorServices();
            };

            MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

            await service.StartAsync(CancellationToken.None);

            try
            {
                var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;
                IMetricsResponse result = ep.Invoke(null);
                Assert.NotNull(result);

                Assert.IsType<MetricsListNamesResponse>(result);
                var resp = result as MetricsListNamesResponse;
                Assert.Empty(resp.Names);
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
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>();

            Counter<double> testMeasure = SteeltoeMetrics.Meter.CreateCounter<double>("test.test5");
            long allKeysSum = 0;

            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            for (int i = 0; i < 10; i++)
            {
                allKeysSum += i;
                testMeasure.Add(i, labels.AsReadonlySpan());
            }

            List<KeyValuePair<string, string>> tags = labels.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).ToList();
            var req = new MetricsRequest("test.test5", tags);
            var resp = ep.Invoke(req) as MetricsResponse;
            Assert.NotNull(resp);

            Assert.Equal("test.test5", resp.Name);

            Assert.NotNull(resp.Measurements);
            Assert.Single(resp.Measurements);

            MetricSample sample = resp.Measurements.SingleOrDefault(x => x.Statistic == MetricStatistic.Rate);
            Assert.NotNull(sample);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(resp.AvailableTags);
            Assert.Equal(3, resp.AvailableTags.Count);

            req = new MetricsRequest("foo.bar", tags);
            resp = ep.Invoke(req) as MetricsResponse;
            Assert.Null(resp);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Invoke_WithMetricsRequest_ReturnsExpected_IncudesAdditionalInstruments()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["management:endpoints:metrics:includedmetrics:0"] = "AdditionalTestMeter:AdditionalInstrument"
            });
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuator();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;

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

            for (int i = 0; i < 10; i++)
            {
                allKeysSum += i;
                testMeasure.Add(i, labels.AsReadonlySpan());
                additionalInstrument.Add(i, labels.AsReadonlySpan());
            }

            List<KeyValuePair<string, string>> tags = labels.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).ToList();
            var req = new MetricsRequest("test.test5", tags);
            var resp = ep.Invoke(req) as MetricsResponse;
            Assert.NotNull(resp);

            Assert.Equal("test.test5", resp.Name);

            Assert.NotNull(resp.Measurements);
            Assert.Single(resp.Measurements);

            MetricSample sample = resp.Measurements.SingleOrDefault(x => x.Statistic == MetricStatistic.Rate);
            Assert.NotNull(sample);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(resp.AvailableTags);
            Assert.Equal(3, resp.AvailableTags.Count);

            req = new MetricsRequest("AdditionalInstrument", tags);
            resp = ep.Invoke(req) as MetricsResponse;
            Assert.NotNull(resp);

            Assert.Equal("AdditionalInstrument", resp.Name);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetMetricSamples_ReturnsExpectedCounter()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;

            Counter<double> counter = SteeltoeMetrics.Meter.CreateCounter<double>("test.test7");
            counter.Add(100);

            (MetricsCollection<List<MetricSample>> measurements, _) = ep.GetMetrics();
            Assert.NotNull(measurements);
            Assert.Single(measurements.Values);
            MetricSample sample = measurements.Values.FirstOrDefault()[0];
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
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;
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

            (_, MetricsCollection<List<MetricTag>> tagDictionary) = ep.GetMetrics();

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

            (_, tagDictionary) = ep.GetMetrics();

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
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>() as MetricsEndpoint;

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

            for (int i = 0; i < 10; i++)
            {
                allKeysSum += i;
                testMeasure.Record(i, context1.AsReadonlySpan());
            }

            long aSum = 0;

            for (int i = 0; i < 10; i++)
            {
                aSum += i;
                testMeasure.Record(i, context2.AsReadonlySpan());
            }

            long bSum = 0;

            for (int i = 0; i < 10; i++)
            {
                bSum += i;
                testMeasure.Record(i, context3.AsReadonlySpan());
            }

            long cSum = 0;

            for (int i = 0; i < 10; i++)
            {
                cSum += i;
                testMeasure.Record(i, context4.AsReadonlySpan());
            }

            (MetricsCollection<List<MetricSample>> measurements, _) = ep.GetMetrics();
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

            IList<MetricSample> result = ep.GetMetricSamplesByTags(measurements, "test.test1", aTags);
            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];
            Assert.Equal(allKeysSum + aSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var bTags = new List<KeyValuePair<string, string>>
            {
                new("b", "v1")
            };

            result = ep.GetMetricSamplesByTags(measurements, "test.test1", bTags);

            Assert.NotNull(result);
            Assert.Single(result);

            sample = result[0];

            Assert.Equal(allKeysSum + bSum, sample.Value);
            Assert.Equal(MetricStatistic.Total, sample.Statistic);

            var cTags = new List<KeyValuePair<string, string>>
            {
                new("c", "v1")
            };

            result = ep.GetMetricSamplesByTags(measurements, "test.test1", cTags);
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

            result = ep.GetMetricSamplesByTags(measurements, "test.test1", abTags);

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

            result = ep.GetMetricSamplesByTags(measurements, "test.test1", acTags);

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

            result = ep.GetMetricSamplesByTags(measurements, "test.test1", bcTags);

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
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddMetricsActuatorServices();
        };

        MetricCollectionHostedService service = tc.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().FirstOrDefault();

        await service.StartAsync(CancellationToken.None);

        try
        {
            var ep = tc.GetService<IMetricsEndpoint>();

            Counter<double> testMeasure = SteeltoeMetrics.Meter.CreateCounter<double>("test.total");

            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            double allKeysSum = 0;

            for (double i = 0; i < 10; i++)
            {
                allKeysSum += i;
                testMeasure.Add(i, labels.AsReadonlySpan());
            }

            var req = new MetricsRequest("test.total", labels.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).ToList());

            var resp = ep.Invoke(req) as MetricsResponse;

            Assert.NotNull(resp);

            Assert.Equal("test.total", resp.Name);

            Assert.NotNull(resp.Measurements);
            Assert.Single(resp.Measurements);
            MetricSample sample = resp.Measurements[0];
            Assert.Equal(MetricStatistic.Rate, sample.Statistic);
            Assert.Equal(allKeysSum, sample.Value);

            Assert.NotNull(resp.AvailableTags);
            Assert.Equal(3, resp.AvailableTags.Count);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }
}

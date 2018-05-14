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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Steeltoe.Common.Http;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class CloudFoundryForwarderExporter : IMetricsExporter
    {
        private const int UNPROCESSABLE_ENTITY = 422;
        private const int PAYLOAD_TOO_LARGE = 413;
        private const int TOO_MANY_REQUESTS = 429;

        private CloudFoundryForwarderOptions settings;
        private IStats stats;
        private IViewManager viewManager;
        private Thread workerThread;
        private bool shutdown = false;
        private ILogger<CloudFoundryForwarderExporter> logger;

        public CloudFoundryForwarderExporter(CloudFoundryForwarderOptions settings, IStats stats, ILogger<CloudFoundryForwarderExporter> logger = null)
        {
            this.settings = settings;
            this.stats = stats;
            this.viewManager = stats.ViewManager;
            this.logger = logger;
        }

        public void Start()
        {
            workerThread = new Thread(this.Run)
            {
                IsBackground = true,
                Name = "MetricsPublisher"
            };
            workerThread.Start();
        }

        public void Stop()
        {
            shutdown = true;
        }

        private void Run(object obj)
        {
            logger?.LogInformation("Exporting metrics to metrics forwarder service");

            while (!shutdown)
            {
                try
                {
                    long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    HttpClient client = GetHttpClient();
                    var requestUri = new Uri(settings.Endpoint);
                    var request = GetHttpRequestMessage(HttpMethod.Post, requestUri);
                    Message message = GetMessage(viewManager.AllExportedViews, timeStamp);
                    request.Content = GetRequestContent(message);

                    DoPost(client, request);

                    Thread.Sleep(settings.RateMilli);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Exception exporting metrics, terminating metrics forwarding!");
                    shutdown = true;
                }
            }
        }

        private async void DoPost(HttpClient client, HttpRequestMessage request)
        {
            HttpClientHelper.ConfigureCertificateValidatation(
                settings.ValidateCertificates,
                out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    logger?.LogDebug("DoPost {0}, status: {1}", request.RequestUri, response.StatusCode);
                    if (response.StatusCode != HttpStatusCode.OK &&
                        response.StatusCode != HttpStatusCode.Accepted)
                    {
                        var headers = response.Headers;
                        var statusCode = (int)response.StatusCode;

                        if (statusCode == UNPROCESSABLE_ENTITY)
                        {
                            logger?.LogError("Failed to send metrics to Metrics Forwarder service due to unprocessable payload.  Discarding metrics.");
                        }
                        else if (statusCode == PAYLOAD_TOO_LARGE)
                        {
                            logger?.LogError("Failed to send metrics to Metrics Forwarder service due to rate limiting.  Discarding metrics.");
                        }
                        else if (statusCode == TOO_MANY_REQUESTS)
                        {
                            logger?.LogError("Failed to send metrics to Metrics Forwarder service due to rate limiting.  Discarding metrics.");
                        }
                        else
                        {
                            logger?.LogError("Failed to send metrics to Metrics Forwarder service. Discarding metrics.  StatusCode: {0}", statusCode);
                        }
                    }

                    return;
                }
            }
            catch (Exception e)
            {
                logger?.LogError("DoPost Exception:", e);
            }
            finally
            {
                client.Dispose();
                HttpClientHelper.RestoreCertificateValidation(settings.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        private Message GetMessage(ISet<IView> exportedViews, long timeStamp)
        {
            IList<IView> views = new List<IView>(exportedViews);
            Instance instance = new Instance(GetInstanceId(), GetInstanceIndex(), GetMetrics(views, timeStamp));
            Application application = new Application(settings.ApplicationId, new List<Instance>() { instance });
            return new Message(new List<Application>() { application });
        }

        private string GetInstanceId()
        {
            if (string.IsNullOrEmpty(settings.InstanceId))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_GUID");
            }

            return settings.InstanceId;
        }

        private string GetInstanceIndex()
        {
            if (string.IsNullOrEmpty(settings.InstanceIndex))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX");
            }

            return settings.InstanceIndex;
        }

        private IList<Metric> GetMetrics(IList<IView> exportedViews, long timeStamp)
        {
            var result = new List<Metric>();
            foreach (var view in exportedViews)
            {
                IViewData data = viewManager.GetView(view.Name);
                if (data != null)
                {
                    IList<Metric> metrics = CreateMetricsFromViewData(data, timeStamp);
                    if (metrics != null)
                    {
                        result.AddRange(metrics);
                    }
                }
            }

            ResetMetrics();

            return result;
        }

        private void ResetMetrics()
        {
            stats.State = StatsCollectionState.DISABLED;
            stats.State = StatsCollectionState.ENABLED;
        }

        private IList<Metric> CreateMetricsFromViewData(IViewData data, long timeStamp)
        {
            IView view = data.View;
            MetricType type = GetMetricType(view.Aggregation);

            string unit = view.Measure.Unit;
            string name = view.Measure.Name;
            var duration = data.End.SubtractTimestamp(data.Start);

            List<Metric> result = new List<Metric>();
            foreach (var entry in data.AggregationMap)
            {
                var tagValues = entry.Key;
                var aggData = entry.Value;
                IDictionary<string, string> tags = MatchTagKeyAndValues(view, tagValues);

                IList<Metric> metrics = GetMetrics(name, type, timeStamp, unit, tags, aggData, duration);
                result.AddRange(metrics);
            }

            return result;
        }

        private IList<Metric> GetMetrics(string measureName, MetricType type, long timeStamp, string unit, IDictionary<string, string> tags, IAggregationData aggregationData, IDuration period)
        {
            List<Metric> results = new List<Metric>();

            // TODO: Seems this is only type supported by PCF
            type = MetricType.GAUGE;

            aggregationData.Match<object>(
                (arg) =>
                {
                    results.AddRange(CreateMetrics(measureName, type, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    results.AddRange(CreateMetrics(measureName, type, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    results.AddRange(CreateMetrics(measureName, type, timeStamp, unit, tags, arg.Count));
                    return null;
                },
                (arg) =>
                {
                    results.AddRange(CreateMetrics(measureName, ".samples", type, timeStamp, "long", tags, arg.Count));
                    results.AddRange(CreateMetrics(measureName, ".mean", type, timeStamp, unit, tags, arg.Mean));
                    return null;
                },
                (arg) =>
                {
                    results.AddRange(CreateMetrics(measureName, ".samples", type, timeStamp, "long", tags, arg.Count));
                    results.AddRange(CreateMetrics(measureName, ".mean", type, timeStamp, unit, tags, arg.Mean));
                    results.AddRange(CreateMetrics(measureName, ".min", type, timeStamp, unit, tags, arg.Min));
                    results.AddRange(CreateMetrics(measureName, ".max", type, timeStamp, unit, tags, arg.Max));

                    var stdDeviation = Math.Sqrt((arg.SumOfSquaredDeviations / arg.Count) - 1);
                    if (double.IsNaN(stdDeviation))
                    {
                        stdDeviation = 0.0;
                    }

                    results.AddRange(CreateMetrics(measureName, ".stddev", type, timeStamp, unit, tags, stdDeviation));

                    return null;
                },
                (arg) =>
                {
                    return null;
                });
            return results;
        }

        private IList<Metric> CreateMetrics(string measureName, MetricType type, long timeStamp, string unit, IDictionary<string, string> tags, double value)
        {
            return CreateMetrics(measureName, string.Empty, type, timeStamp, unit, tags, value);
        }

        private IList<Metric> CreateMetrics(string measureName, string metricNameSuffix, MetricType type, long timeStamp, string unit, IDictionary<string, string> tags, double value)
        {
            List<Metric> results = new List<Metric>();
            if (tags.Count == 0)
            {
                results.Add(new Metric(measureName + metricNameSuffix, type, timeStamp, unit, tags, value));
            }
            else
            {
                foreach (var tag in tags)
                {
                    var metricName = MakeMetricName(measureName, tag.Value) + metricNameSuffix;
                    results.Add(new Metric(metricName, type, timeStamp, unit, new Dictionary<string, string>() { { tag.Key, tag.Value } }, value));
                }
            }

            return results;
        }

        private string MakeMetricName(string measureName, string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                return measureName + "." + tag;
            }

            return measureName;
        }

        private IDictionary<string, string> MatchTagKeyAndValues(IView view, TagValues tagValues)
        {
            var keys = view.Columns;
            var values = tagValues.Values;
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (keys.Count != values.Count)
            {
                logger?.LogWarning("TagKeys and TagValues don't have same size., ignoring tags");
                return result;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var val = values[i];
                result[key.Name] = val.AsString;
            }

            return result;
        }

        private double GetMetricValue(IAggregationData aggregationData)
        {
            return aggregationData.Match(
                (arg) => { return arg.Sum; },
                (arg) => { return arg.Sum; },
                (arg) => { return arg.Count; },
                (arg) => { return arg.Mean; },
                (arg) => { return arg.Mean; },
                (arg) => { return 0.0; });
        }

        private MetricType GetMetricType(IAggregation aggregation)
        {
            return aggregation.Match(
                (arg) => { return MetricType.COUNTER; },
                (arg) => { return MetricType.COUNTER; },
                (arg) => { return MetricType.GAUGE; },
                (arg) => { return MetricType.GAUGE; },
                (arg) => { return MetricType.UNKNOWN; });
        }

        private HttpRequestMessage GetHttpRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Authorization", settings.AccessToken);
            request.Headers.Add("Accept", "application/json");
            return request;
        }

        private HttpContent GetRequestContent(Message toSerialize)
        {
            try
            {
                string json = JsonConvert.SerializeObject(toSerialize);
                logger?.LogDebug("GetRequestContent generated JSON: {0}", json);
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                logger?.LogError("GetRequestContent Exception: {0}", e);
            }

            return new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }

        private HttpClient GetHttpClient()
        {
            return HttpClientHelper.GetHttpClient(settings.ValidateCertificates, settings.TimeoutSeconds * 1000);
        }
    }
}

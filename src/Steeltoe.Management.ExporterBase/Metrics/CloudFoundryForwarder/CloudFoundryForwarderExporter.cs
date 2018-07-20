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

        private readonly ILogger<CloudFoundryForwarderExporter> logger;
        private readonly ICloudFoundryMetricWriter metricFormatWriter;

        private CloudFoundryForwarderOptions options;
        private IStats stats;
        private IViewManager viewManager;
        private Thread workerThread;
        private bool shutdown = false;

        public CloudFoundryForwarderExporter(CloudFoundryForwarderOptions options, IStats stats, ILogger<CloudFoundryForwarderExporter> logger = null)
        {
            this.options = options;
            this.stats = stats;
            this.viewManager = stats.ViewManager;
            this.logger = logger;
            if (options.MicrometerMetricWriter)
            {
                this.metricFormatWriter = new MicrometerMetricWriter(options, stats, logger);
            }
            else
            {
                this.metricFormatWriter = new SpringBootMetricWriter(options, stats, logger);
            }
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

        protected internal void Run(object obj)
        {
            logger?.LogInformation("Exporting metrics to metrics forwarder service");

            while (!shutdown)
            {
                try
                {
                    long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    HttpClient client = GetHttpClient();
                    var requestUri = new Uri(options.Endpoint);
                    var request = GetHttpRequestMessage(HttpMethod.Post, requestUri);
                    Message message = GetMessage(viewManager.AllExportedViews, timeStamp);
                    request.Content = GetRequestContent(message);

                    DoPost(client, request);

                    Thread.Sleep(options.RateMilli);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Exception exporting metrics, terminating metrics forwarding!");
                    shutdown = true;
                }
            }
        }

        protected internal async void DoPost(HttpClient client, HttpRequestMessage request)
        {
            HttpClientHelper.ConfigureCertificateValidatation(
                options.ValidateCertificates,
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
                            logger?.LogError("Failed to send metrics to Metrics Forwarder service. Discarding metrics.  StatusCode: {status}", statusCode);
                        }
                    }

                    return;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "DoPost Exception: {uri}", request.RequestUri);
            }
            finally
            {
                client.Dispose();
                HttpClientHelper.RestoreCertificateValidation(options.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        protected internal Message GetMessage(ISet<IView> exportedViews, long timeStamp)
        {
            Instance instance = new Instance(GetInstanceId(), GetInstanceIndex(), GetMetricsForExportedViews(exportedViews, timeStamp));
            Application application = new Application(options.ApplicationId, new List<Instance>() { instance });
            return new Message(new List<Application>() { application });
        }

        protected internal IList<Metric> GetMetricsForExportedViews(ISet<IView> exportedViews, long timeStamp)
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

        protected internal IList<Metric> CreateMetricsFromViewData(IViewData viewData, long timeStamp)
        {
            List<Metric> result = new List<Metric>();
            foreach (var entry in viewData.AggregationMap)
            {
                IList<Metric> metrics = metricFormatWriter.CreateMetrics(viewData, entry.Value, entry.Key, timeStamp);
                result.AddRange(metrics);
            }

            return result;
        }

        protected internal string GetInstanceId()
        {
            if (string.IsNullOrEmpty(options.InstanceId))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_GUID");
            }

            return options.InstanceId;
        }

        protected internal string GetInstanceIndex()
        {
            if (string.IsNullOrEmpty(options.InstanceIndex))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX");
            }

            return options.InstanceIndex;
        }

        protected internal HttpRequestMessage GetHttpRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Authorization", options.AccessToken);
            request.Headers.Add("Accept", "application/json");
            logger?.LogDebug("GetHttpRequestMessage {0}, token: {1}", request.RequestUri, options.AccessToken);
            return request;
        }

        protected internal HttpContent GetRequestContent(Message toSerialize)
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

        protected internal HttpClient GetHttpClient()
        {
            return HttpClientHelper.GetHttpClient(options.ValidateCertificates, options.TimeoutSeconds * 1000);
        }

        protected internal void ResetMetrics()
        {
            stats.State = StatsCollectionState.DISABLED;
            stats.State = StatsCollectionState.ENABLED;
        }
    }
}

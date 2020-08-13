// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenCensus.Stats;
using Steeltoe.Common.Http;
using Steeltoe.Management.Census.Stats;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class CloudFoundryForwarderExporter : IMetricsExporter
    {
        private const int UNPROCESSABLE_ENTITY = 422;
        private const int PAYLOAD_TOO_LARGE = 413;
        private const int TOO_MANY_REQUESTS = 429;
        private static HttpClient _httpClient;

        private readonly ILogger<CloudFoundryForwarderExporter> _logger;
        private readonly ICloudFoundryMetricWriter _metricFormatWriter;

        private readonly CloudFoundryForwarderOptions _options;
        private readonly IStats _stats;
        private readonly IViewManager _viewManager;
        private Thread _workerThread;
        private bool _shutdown = false;

        public CloudFoundryForwarderExporter(CloudFoundryForwarderOptions options, IStats stats, ILogger<CloudFoundryForwarderExporter> logger = null)
        {
            _options = options;
            _stats = stats;
            _viewManager = stats.ViewManager;
            _httpClient ??= GetHttpClient();
            _logger = logger;
            if (options.MicrometerMetricWriter)
            {
                _metricFormatWriter = new MicrometerMetricWriter(options, stats, logger);
            }
            else
            {
                _metricFormatWriter = new SpringBootMetricWriter(options, stats, logger);
            }
        }

        public void Start()
        {
            _workerThread = new Thread(Run)
            {
                IsBackground = true,
                Name = "MetricsPublisher"
            };
            _workerThread.Start();
        }

        public void Stop()
        {
            _shutdown = true;
        }

        protected internal void Run(object obj)
        {
            if (string.IsNullOrEmpty(_options.AccessToken) || string.IsNullOrEmpty(_options.Endpoint))
            {
                _logger?.LogInformation("Unable to export metrics to metrics forwarder service, service binding missing!");
                Stop();
            }
            else
            {
                _logger?.LogInformation("Exporting metrics to metrics forwarder service");
            }

            while (!_shutdown)
            {
                try
                {
                    var timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    var requestUri = new Uri(_options.Endpoint);
                    var request = GetHttpRequestMessage(HttpMethod.Post, requestUri);
                    var message = GetMessage(_viewManager.AllExportedViews, timeStamp);
                    request.Content = GetRequestContent(message);

                    DoPost(_httpClient, request);

                    Thread.Sleep(_options.RateMilli);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Exception exporting metrics, terminating metrics forwarding!");
                    _shutdown = true;
                }
            }
        }

        // fire and forget
#pragma warning disable S3168 // "async" methods should not return "void"
        protected internal async void DoPost(HttpClient client, HttpRequestMessage request)
#pragma warning restore S3168 // "async" methods should not return "void"
        {
            HttpClientHelper.ConfigureCertificateValidation(
                _options.ValidateCertificates,
                out var prevProtocols,
                out var prevValidator);
            try
            {
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                _logger?.LogDebug("DoPost {0}, status: {1}", request.RequestUri, response.StatusCode);
                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.Accepted)
                {
                    _ = response.Headers;
                    var statusCode = (int)response.StatusCode;

                    if (statusCode == UNPROCESSABLE_ENTITY)
                    {
                        _logger?.LogError("Failed to send metrics to Metrics Forwarder service due to unprocessable payload.  Discarding metrics.");
                    }
                    else if (statusCode == PAYLOAD_TOO_LARGE)
                    {
                        _logger?.LogError("Failed to send metrics to Metrics Forwarder service due to rate limiting.  Discarding metrics.");
                    }
                    else if (statusCode == TOO_MANY_REQUESTS)
                    {
                        _logger?.LogError("Failed to send metrics to Metrics Forwarder service due to rate limiting.  Discarding metrics.");
                    }
                    else
                    {
                        _logger?.LogError("Failed to send metrics to Metrics Forwarder service. Discarding metrics.  StatusCode: {status}", statusCode);
                    }
                }

                return;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "DoPost Exception: {uri}", request.RequestUri);
            }
            finally
            {
                client.Dispose();
                HttpClientHelper.RestoreCertificateValidation(_options.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        protected internal Message GetMessage(ISet<IView> exportedViews, long timeStamp)
        {
            var instance = new Instance(GetInstanceId(), GetInstanceIndex(), GetMetricsForExportedViews(exportedViews, timeStamp));
            var application = new Application(_options.ApplicationId, new List<Instance>() { instance });
            return new Message(new List<Application>() { application });
        }

        protected internal IList<Metric> GetMetricsForExportedViews(ISet<IView> exportedViews, long timeStamp)
        {
            var result = new List<Metric>();
            foreach (var view in exportedViews)
            {
                var data = _viewManager.GetView(view.Name);
                if (data != null)
                {
                    var metrics = CreateMetricsFromViewData(data, timeStamp);
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
            var result = new List<Metric>();
            foreach (var entry in viewData.AggregationMap)
            {
                var metrics = _metricFormatWriter.CreateMetrics(viewData, entry.Value, entry.Key, timeStamp);
                result.AddRange(metrics);
            }

            return result;
        }

        protected internal string GetInstanceId()
        {
            if (string.IsNullOrEmpty(_options.InstanceId))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_GUID");
            }

            return _options.InstanceId;
        }

        protected internal string GetInstanceIndex()
        {
            if (string.IsNullOrEmpty(_options.InstanceIndex))
            {
                return Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX");
            }

            return _options.InstanceIndex;
        }

        protected internal HttpRequestMessage GetHttpRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Authorization", _options.AccessToken);
            request.Headers.Add("Accept", "application/json");
            _logger?.LogDebug("GetHttpRequestMessage {0}, token: {1}", request.RequestUri, _options.AccessToken);
            return request;
        }

        protected internal HttpContent GetRequestContent(Message toSerialize)
        {
            try
            {
                var json = JsonConvert.SerializeObject(toSerialize);
                _logger?.LogDebug("GetRequestContent generated JSON: {0}", json);
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                _logger?.LogError("GetRequestContent Exception: {0}", e);
            }

            return new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }

        protected internal HttpClient GetHttpClient()
        {
            return HttpClientHelper.GetHttpClient(_options.ValidateCertificates, _options.TimeoutSeconds * 1000);
        }

        protected internal void ResetMetrics()
        {
            _stats.State = StatsCollectionState.DISABLED;
            _stats.State = StatsCollectionState.ENABLED;
        }
    }
}

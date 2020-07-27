﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenCensus.Common;
using OpenCensus.Trace;
using OpenCensus.Trace.Export;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    [Obsolete("Use OpenCensus project packages")]
    internal class TraceExporterHandler : IHandler
    {
        private const string STATUS_CODE = "census.status_code";
        private const string STATUS_DESCRIPTION = "census.status_description";

        private const long MillisPerSecond = 1000L;
        private const long NanosPerMillisecond = 1000 * 1000;
        private const long NanosPerSecond = NanosPerMillisecond * MillisPerSecond;
        private readonly ITraceExporterOptions _options;
        private readonly ILogger _logger;
        private readonly ZipkinEndpoint _localEndpoint;

        public TraceExporterHandler(ITraceExporterOptions options, ILogger logger = null)
        {
            _options = options;
            _logger = logger;
            _localEndpoint = GetLocalZipkinEndpoint();
        }

        public void Export(IEnumerable<ISpanData> spanDataList)
        {
            var zipkinSpans = new List<ZipkinSpan>();
            foreach (var data in spanDataList)
            {
                var zipkinSpan = GenerateSpan(data, _localEndpoint);
                zipkinSpans.Add(zipkinSpan);
            }

            SendSpansAsync(zipkinSpans);
        }

        internal ZipkinSpan GenerateSpan(ISpanData spanData, ZipkinEndpoint localEndpoint)
        {
            var context = spanData.Context;
            var startTimestamp = ToEpochMicroseconds(spanData.StartTimestamp);
            var endTimestamp = ToEpochMicroseconds(spanData.EndTimestamp);

            var spanBuilder =
                ZipkinSpan.NewBuilder()
                    .TraceId(EncodeTraceId(context.TraceId))
                    .Id(EncodeSpanId(context.SpanId))
                    .Kind(ToSpanKind(spanData))
                    .Name(spanData.Name)
                    .Timestamp(ToEpochMicroseconds(spanData.StartTimestamp))
                    .Duration(endTimestamp - startTimestamp)
                    .LocalEndpoint(localEndpoint);

            if (spanData.ParentSpanId != null && spanData.ParentSpanId.IsValid)
            {
                spanBuilder.ParentId(EncodeSpanId(spanData.ParentSpanId));
            }

            foreach (var label in spanData.Attributes.AttributeMap)
            {
                spanBuilder.PutTag(label.Key, AttributeValueToString(label.Value));
            }

            var status = spanData.Status;
            if (status != null)
            {
                spanBuilder.PutTag(STATUS_CODE, status.CanonicalCode.ToString());
                if (status.Description != null)
                {
                    spanBuilder.PutTag(STATUS_DESCRIPTION, status.Description);
                }
            }

            foreach (var annotation in spanData.Annotations.Events)
            {
                spanBuilder.AddAnnotation(
                    ToEpochMicroseconds(annotation.Timestamp), annotation.Event.Description);
            }

            foreach (var networkEvent in spanData.MessageEvents.Events)
            {
                spanBuilder.AddAnnotation(
                    ToEpochMicroseconds(networkEvent.Timestamp), networkEvent.Event.Type.ToString());
            }

            return spanBuilder.Build();
        }

        private long ToEpochMicroseconds(ITimestamp timestamp)
        {
            var nanos = (timestamp.Seconds * NanosPerSecond) + timestamp.Nanos;
            var micros = nanos / 1000L;
            return micros;
        }

        private string AttributeValueToString(IAttributeValue attributeValue)
        {
            return attributeValue.Match(
                (arg) => { return arg; },
                (arg) => { return arg.ToString(); },
                (arg) => { return arg.ToString(); },
                (arg) => { return arg.ToString(); },
                (arg) => { return null; });
        }

        private string EncodeTraceId(ITraceId traceId)
        {
            var id = traceId.ToLowerBase16();
            if (id.Length > 16 && _options.UseShortTraceIds)
            {
                id = id.Substring(id.Length - 16, 16);
            }

            return id;
        }

        private string EncodeSpanId(ISpanId spanId)
        {
            return spanId.ToLowerBase16();
        }

        private ZipkinSpanKind ToSpanKind(ISpanData spanData)
        {
            if (spanData.Kind == SpanKind.Server)
            {
                return ZipkinSpanKind.SERVER;
            }

            return ZipkinSpanKind.CLIENT;
        }

        // fire and forget
#pragma warning disable S3168 // "async" methods should not return "void"
        private async void SendSpansAsync(List<ZipkinSpan> spans)
#pragma warning restore S3168 // "async" methods should not return "void"
        {
            try
            {
                var client = GetHttpClient();
                var requestUri = new Uri(_options.Endpoint);
                var request = GetHttpRequestMessage(HttpMethod.Post, requestUri);
                request.Content = GetRequestContent(spans);

                await DoPost(client, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception exporting traces!");
            }
        }

        private async Task DoPost(HttpClient client, HttpRequestMessage request)
        {
            HttpClientHelper.ConfigureCertificateValidation(
                _options.ValidateCertificates,
                out var prevProtocols,
                out var prevValidator);
            try
            {
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                _logger?.LogDebug("DoPost {uri}, status: {status}", request.RequestUri, response.StatusCode);
                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.Accepted)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger?.LogError("Failed to send traces to Zipkin. Discarding traces.  StatusCode: {status}, Uri: {uri}", statusCode, request.RequestUri);
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

        private HttpRequestMessage GetHttpRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Add("Accept", "application/json");
            return request;
        }

        private HttpContent GetRequestContent(IList<ZipkinSpan> toSerialize)
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

        private HttpClient GetHttpClient()
        {
            return HttpClientHelper.GetHttpClient(_options.ValidateCertificates, _options.TimeoutSeconds * 1000);
        }

        private ZipkinEndpoint GetLocalZipkinEndpoint()
        {
            var result = new ZipkinEndpoint()
            {
                ServiceName = _options.ServiceName
            };

            var hostName = ResolveHostName();
            if (!string.IsNullOrEmpty(hostName))
            {
                result.Ipv4 = ResolveHostAddress(hostName, AddressFamily.InterNetwork);
                result.Ipv6 = ResolveHostAddress(hostName, AddressFamily.InterNetworkV6);
            }

            return result;
        }

        private string ResolveHostAddress(string hostName, AddressFamily family)
        {
            string result = null;
            try
            {
                var results = Dns.GetHostAddresses(hostName);
                if (results != null && results.Length > 0)
                {
                    foreach (var addr in results)
                    {
                        if (addr.AddressFamily.Equals(family))
                        {
                            result = addr.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        private string ResolveHostName()
        {
            string result = null;
            try
            {
                result = Dns.GetHostName();
                if (!string.IsNullOrEmpty(result))
                {
                    var response = Dns.GetHostEntry(result);
                    if (response != null)
                    {
                        return response.HostName;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }
    }
}

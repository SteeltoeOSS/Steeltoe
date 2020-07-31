// <copyright file="TraceExporterHandler.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Exporter.Zipkin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using OpenCensus.Common;
    using OpenCensus.Trace;
    using OpenCensus.Trace.Export;

    internal class TraceExporterHandler : IHandler
    {
        private const string StatusCode = "census.status_code";
        private const string StatusDescription = "census.status_description";
        private const long MillisPerSecond = 1000L;
        private const long NanosPerMillisecond = 1000 * 1000;
        private const long NanosPerSecond = NanosPerMillisecond * MillisPerSecond;
        private readonly ZipkinTraceExporterOptions options;
        private readonly ZipkinEndpoint localEndpoint;
        private readonly HttpClient httpClient;

        public TraceExporterHandler(ZipkinTraceExporterOptions options, HttpClient client)
        {
            this.options = options;
            localEndpoint = GetLocalZipkinEndpoint();
            httpClient = client ?? new HttpClient();
        }

        public void Export(IEnumerable<ISpanData> spanDataList)
        {
            var zipkinSpans = new List<ZipkinSpan>();

            foreach (var data in spanDataList)
            {
                var zipkinSpan = GenerateSpan(data, localEndpoint);
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
                spanBuilder.PutTag(StatusCode, status.CanonicalCode.ToString());

                if (status.Description != null)
                {
                    spanBuilder.PutTag(StatusDescription, status.Description);
                }
            }

            foreach (var annotation in spanData.Annotations.Events)
            {
                spanBuilder.AddAnnotation(ToEpochMicroseconds(annotation.Timestamp), annotation.Event.Description);
            }

            foreach (var networkEvent in spanData.MessageEvents.Events)
            {
                spanBuilder.AddAnnotation(ToEpochMicroseconds(networkEvent.Timestamp), networkEvent.Event.Type.ToString());
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

            if (id.Length > 16 && options.UseShortTraceIds)
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
            else if (spanData.Kind == SpanKind.Client)
            {
                if (spanData.HasRemoteParent.HasValue && spanData.HasRemoteParent.Value)
                {
                    return ZipkinSpanKind.SERVER;
                }

                return ZipkinSpanKind.CLIENT;
            }

            return ZipkinSpanKind.CLIENT;
        }

        private async void SendSpansAsync(List<ZipkinSpan> spans)
        {
            try
            {
                var requestUri = options.Endpoint;
                var request = GetHttpRequestMessage(HttpMethod.Post, requestUri);
                request.Content = GetRequestContent(spans);
                await DoPost(httpClient, request);
            }
            catch (Exception)
            {
            }
        }

        private async Task DoPost(HttpClient client, HttpRequestMessage request)
        {
            try
            {
                using (var response = await client.SendAsync(request))
                {
                    if (response.StatusCode != HttpStatusCode.OK &&
                        response.StatusCode != HttpStatusCode.Accepted)
                    {
                        var statusCode = (int)response.StatusCode;
                    }

                    return;
                }
            }
            catch (Exception)
            {
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

                return new StringContent(json, Encoding.UTF8, "application/json");
            }
            catch (Exception)
            {
            }

            return new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }

        private ZipkinEndpoint GetLocalZipkinEndpoint()
        {
            var result = new ZipkinEndpoint()
            {
                ServiceName = options.ServiceName,
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

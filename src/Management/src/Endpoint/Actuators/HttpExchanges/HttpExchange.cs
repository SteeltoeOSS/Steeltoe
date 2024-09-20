// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using System.Xml;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public sealed class HttpExchange
{
    public DateTime Timestamp { get; }
    public HttpExchangePrincipal? Principal { get; }
    public HttpExchangeSession? Session { get; }
    public HttpExchangeRequest Request { get; }
    public HttpExchangeResponse Response { get; }

    [JsonIgnore]
    public TimeSpan TimeTaken { get; }

    [JsonPropertyName("TimeTaken")]
    internal string SerializedTimeTaken => XmlConvert.ToString(TimeTaken);

    public HttpExchange(HttpExchangeRequest request, HttpExchangeResponse response, DateTime timestamp, HttpExchangePrincipal? principal,
        HttpExchangeSession? session, TimeSpan timeTaken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        Request = request;
        Response = response;
        Timestamp = timestamp;
        Principal = principal;
        Session = session;
        TimeTaken = timeTaken;
    }
}

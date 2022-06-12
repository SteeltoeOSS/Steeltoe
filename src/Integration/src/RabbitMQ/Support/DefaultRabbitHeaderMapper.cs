// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Mapping;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Integration.Rabbit.Support;

public class DefaultRabbitHeaderMapper : AbstractHeaderMapper<IMessageHeaders>, IRabbitHeaderMapper
{
    private static readonly List<string> STANDARD_HEADER_NAMES = new ();

    static DefaultRabbitHeaderMapper()
    {
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.APP_ID);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.CLUSTER_ID);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.CONTENT_ENCODING);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.CONTENT_LENGTH);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.CONTENT_TYPE);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.CORRELATION_ID);

        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.DELAY);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.DELIVERY_MODE);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.DELIVERY_TAG);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.EXPIRATION);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.MESSAGE_COUNT);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.MESSAGE_ID);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.RECEIVED_DELAY);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.RECEIVED_DELIVERY_MODE);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.RECEIVED_EXCHANGE);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.RECEIVED_ROUTING_KEY);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.REDELIVERED);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.REPLY_TO);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.TIMESTAMP);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.TYPE);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.USER_ID);
        STANDARD_HEADER_NAMES.Add(MessageHeaders.TYPE_ID);
        STANDARD_HEADER_NAMES.Add(MessageHeaders.CONTENT_TYPE_ID);
        STANDARD_HEADER_NAMES.Add(MessageHeaders.KEY_TYPE_ID);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.SPRING_REPLY_CORRELATION);
        STANDARD_HEADER_NAMES.Add(RabbitMessageHeaders.SPRING_REPLY_TO_STACK);
    }

    protected DefaultRabbitHeaderMapper(string[] requestHeaderNames, string[] replyHeaderNames, ILogger logger)
        : base(RabbitMessageHeaders.PREFIX, STANDARD_HEADER_NAMES, STANDARD_HEADER_NAMES, logger)
    {
        if (requestHeaderNames != null)
        {
            SetRequestHeaderNames(requestHeaderNames);
        }

        if (replyHeaderNames != null)
        {
            SetReplyHeaderNames(replyHeaderNames);
        }
    }

    protected override IDictionary<string, object> ExtractStandardHeaders(IMessageHeaders source)
    {
        return new Dictionary<string, object>();
    }

    protected override IDictionary<string, object> ExtractUserDefinedHeaders(IMessageHeaders source)
    {
        throw new NotImplementedException();
    }

    protected override void PopulateStandardHeaders(IDictionary<string, object> headers, IMessageHeaders target)
    {
        headers
            .Where(header => STANDARD_HEADER_NAMES.Contains(header.Key) && !string.IsNullOrEmpty(header.Value?.ToString()))
            .ToList()
            .ForEach(header => target.Add(header.Key, header.Value));
    }

    protected override void PopulateUserDefinedHeader(string headerName, object headerValue, IMessageHeaders target)
    {
        if (!target.ContainsKey(headerName)
            && !RabbitMessageHeaders.CONTENT_TYPE.Equals(headerName)
            && !headerName.StartsWith("json"))
        {
            target.Add(headerName, headerValue);
        }
    }

    public static string[] InboundRequestHeaders { get; } = { "*" };

    public static string[] InboundReplyHeaders { get; } = SafeOutboundHeaders;

    public static string[] SafeOutboundHeaders { get; } = { "!x-*", "*" };

    public static string[] OutboundRequestHeaders { get; } = SafeOutboundHeaders;

    public static string[] OutboundReplyHeaders { get; } = { "*" };

    public static DefaultRabbitHeaderMapper GetInboundMapper(ILogger logger) => new (InboundRequestHeaders, InboundReplyHeaders, logger);

    public static DefaultRabbitHeaderMapper GetOutboundMapper(ILogger logger) => new (OutboundRequestHeaders, OutboundReplyHeaders, logger);
}

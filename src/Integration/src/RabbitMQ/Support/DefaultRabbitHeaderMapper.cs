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
    private static readonly List<string> StandardHeaderNames = new ();

    static DefaultRabbitHeaderMapper()
    {
        StandardHeaderNames.Add(RabbitMessageHeaders.AppId);
        StandardHeaderNames.Add(RabbitMessageHeaders.ClusterId);
        StandardHeaderNames.Add(RabbitMessageHeaders.ContentEncoding);
        StandardHeaderNames.Add(RabbitMessageHeaders.ContentLength);
        StandardHeaderNames.Add(RabbitMessageHeaders.ContentType);
        StandardHeaderNames.Add(RabbitMessageHeaders.CorrelationId);

        StandardHeaderNames.Add(RabbitMessageHeaders.Delay);
        StandardHeaderNames.Add(RabbitMessageHeaders.DeliveryMode);
        StandardHeaderNames.Add(RabbitMessageHeaders.DeliveryTag);
        StandardHeaderNames.Add(RabbitMessageHeaders.Expiration);
        StandardHeaderNames.Add(RabbitMessageHeaders.MessageCount);
        StandardHeaderNames.Add(RabbitMessageHeaders.MessageId);
        StandardHeaderNames.Add(RabbitMessageHeaders.ReceivedDelay);
        StandardHeaderNames.Add(RabbitMessageHeaders.ReceivedDeliveryMode);
        StandardHeaderNames.Add(RabbitMessageHeaders.ReceivedExchange);
        StandardHeaderNames.Add(RabbitMessageHeaders.ReceivedRoutingKey);
        StandardHeaderNames.Add(RabbitMessageHeaders.Redelivered);
        StandardHeaderNames.Add(RabbitMessageHeaders.ReplyTo);
        StandardHeaderNames.Add(RabbitMessageHeaders.Timestamp);
        StandardHeaderNames.Add(RabbitMessageHeaders.Type);
        StandardHeaderNames.Add(RabbitMessageHeaders.UserId);
        StandardHeaderNames.Add(MessageHeaders.TypeId);
        StandardHeaderNames.Add(MessageHeaders.ContentTypeId);
        StandardHeaderNames.Add(MessageHeaders.KeyTypeId);
        StandardHeaderNames.Add(RabbitMessageHeaders.SpringReplyCorrelation);
        StandardHeaderNames.Add(RabbitMessageHeaders.SpringReplyToStack);
    }

    protected DefaultRabbitHeaderMapper(string[] requestHeaderNames, string[] replyHeaderNames, ILogger logger)
        : base(RabbitMessageHeaders.Prefix, StandardHeaderNames, StandardHeaderNames, logger)
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
            .Where(header => StandardHeaderNames.Contains(header.Key) && !string.IsNullOrEmpty(header.Value?.ToString()))
            .ToList()
            .ForEach(header => target.Add(header.Key, header.Value));
    }

    protected override void PopulateUserDefinedHeader(string headerName, object headerValue, IMessageHeaders target)
    {
        if (!target.ContainsKey(headerName)
            && !RabbitMessageHeaders.ContentType.Equals(headerName)
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

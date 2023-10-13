// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class DefaultMessageHeadersConverter : IMessageHeadersConverter
{
    private const int DefaultLongStringLimit = 1024;
    private readonly ILogger _logger;

    public DefaultMessageHeadersConverter(ILogger logger = null)
        : this(DefaultLongStringLimit, false)
    {
        _logger = logger;
    }

    public DefaultMessageHeadersConverter(int longStringLimit)
        : this(longStringLimit, false)
    {
    }

    public DefaultMessageHeadersConverter(int longStringLimit, bool convertLongLongStrings)
    {
    }

    public virtual void FromMessageHeaders(IMessageHeaders source, RC.IBasicProperties target, Encoding charset)
    {
        target.Headers = ConvertHeadersIfNecessary(source);

        if (source.Timestamp.HasValue)
        {
            target.Timestamp = new RC.AmqpTimestamp(source.Timestamp.Value);
        }

        if (source.Id != null)
        {
            target.MessageId = source.Id;
        }

        if (source.UserId() != null)
        {
            target.UserId = source.UserId();
        }

        if (source.AppId() != null)
        {
            target.AppId = source.AppId();
        }

        if (source.ClusterId() != null)
        {
            target.ClusterId = source.ClusterId();
        }

        if (source.Type() != null)
        {
            target.Type = source.Type();
        }

        if (source.DeliveryMode().HasValue)
        {
            target.DeliveryMode = (byte)source.DeliveryMode().Value;
        }
        else
        {
            target.DeliveryMode = (byte)RabbitHeaderAccessor.DefaultDeliveryMode;
        }

        if (source.Expiration() != null)
        {
            target.Expiration = source.Expiration();
        }

        if (source.Priority().HasValue)
        {
            target.Priority = (byte)source.Priority().Value;
        }

        if (source.ContentType() != null)
        {
            target.ContentType = source.ContentType();
        }

        if (source.ContentEncoding() != null)
        {
            target.ContentEncoding = source.ContentEncoding();
        }

        string correlationId = source.CorrelationId();

        if (!string.IsNullOrEmpty(correlationId))
        {
            target.CorrelationId = correlationId;
        }

        string replyTo = source.ReplyTo();

        if (replyTo != null)
        {
            target.ReplyTo = replyTo;
        }
    }

    public virtual IMessageHeaders ToMessageHeaders(RC.IBasicProperties source, Envelope envelope, Encoding charset)
    {
        var target = new RabbitHeaderAccessor();
        IDictionary<string, object> headers = source.Headers;

        if (headers?.Count > 0)
        {
            foreach (KeyValuePair<string, object> entry in headers)
            {
                string key = entry.Key;

                if (key == RabbitMessageHeaders.XDelay)
                {
                    object value = entry.Value;

                    if (value is int intVal)
                    {
                        target.ReceivedDelay = intVal;
                    }
                }
                else
                {
                    target.SetHeader(key, ConvertLongStringIfNecessary(entry.Value, charset));
                }
            }
        }

        target.Timestamp = source.Timestamp.UnixTime;
        target.MessageId = source.MessageId;
        target.ReceivedUserId = source.UserId;
        target.AppId = source.AppId;
        target.ClusterId = source.ClusterId;
        target.Type = source.Type;
        int deliveryMode = source.DeliveryMode;
        target.ReceivedDeliveryMode = (MessageDeliveryMode)Enum.ToObject(typeof(MessageDeliveryMode), deliveryMode);
        target.DeliveryMode = null;
        target.Expiration = source.Expiration;
        target.Priority = source.Priority;

        target.ContentType = source.ContentType;

        target.ContentEncoding = source.ContentEncoding;
        string correlationId = source.CorrelationId;

        if (!string.IsNullOrEmpty(correlationId))
        {
            target.CorrelationId = correlationId;
        }

        string replyTo = source.ReplyTo;

        if (replyTo != null)
        {
            target.ReplyTo = replyTo;
        }

        if (envelope != null)
        {
            target.ReceivedExchange = envelope.Exchange;
            target.ReceivedRoutingKey = envelope.RoutingKey;
            target.Redelivered = envelope.Redeliver;
            target.DeliveryTag = envelope.DeliveryTag;
        }

        target.LeaveMutable = true;
        return target.MessageHeaders;
    }

    private IDictionary<string, object> ConvertHeadersIfNecessary(IDictionary<string, object> source)
    {
        if (source.Count == 0)
        {
            return source;
        }

        var writableHeaders = new Dictionary<string, object>();

        foreach (KeyValuePair<string, object> entry in source)
        {
            if (!entry.Key.StartsWith(MessageHeaders.Internal, StringComparison.Ordinal) &&
                !entry.Key.StartsWith(RabbitMessageHeaders.RabbitProperty, StringComparison.Ordinal) && entry.Key != MessageHeaders.IdName &&
                entry.Key != MessageHeaders.TimestampName)
            {
                writableHeaders[entry.Key] = ConvertHeaderValueIfNecessary(entry.Value);
            }
        }

        return writableHeaders;
    }

    private object ConvertHeaderValueIfNecessary(object valueArg)
    {
        object value = valueArg;

        if (value == null)
        {
            return null;
        }

        bool isValid = value switch
        {
            string => true,
            byte[] => true,
            bool => true,
            byte => true,
            sbyte => true,
            uint => true,
            int => true,
            long => true,
            float => true,
            double => true,
            decimal => true,
            short => true,
            RC.AmqpTimestamp => true,
            IList => true,
            IDictionary => true,
            RC.BinaryTableValue => true,
            Type => true,
            _ => false
        };

        if (!isValid)
        {
            value = value.ToString();
        }
        else if (value is byte[] v)
        {
            value = new RC.BinaryTableValue(v);
        }
        else if (value is object[] array)
        {
            object[] writableList = new object[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                writableList[i] = ConvertHeaderValueIfNecessary(array[i]);
            }

            value = writableList;
        }
        else if (value is IList list)
        {
            var writableList = new List<object>();

            foreach (object listValue in list)
            {
                writableList.Add(ConvertHeaderValueIfNecessary(listValue));
            }

            value = writableList;
        }
        else if (value is IDictionary originalMap)
        {
            var writableMap = new Dictionary<object, object>();

            foreach (DictionaryEntry entry in originalMap)
            {
                writableMap[entry.Key] = ConvertHeaderValueIfNecessary(entry.Value);
            }

            value = writableMap;
        }
        else if (value is Type type)
        {
            value = type.ToString();
        }

        return value;
    }

    private object ConvertLongStringIfNecessary(object valueArg, Encoding charset)
    {
        switch (valueArg)
        {
            case byte[] byteValue:

                try
                {
                    return charset.GetString(byteValue);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, ex.Message);
                }

                break;

            case List<object> listValue:

                var convertedList = new List<object>();

                foreach (object item in listValue)
                {
                    convertedList.Add(ConvertLongStringIfNecessary(item, charset));
                }

                return convertedList;

            case IDictionary<string, object> dictValue:

                var convertedMap = new Dictionary<string, object>();

                foreach (KeyValuePair<string, object> entry in dictValue)
                {
                    convertedMap.Add(entry.Key, ConvertLongStringIfNecessary(entry.Value, charset));
                }

                return convertedMap;
        }

        return valueArg;
    }
}

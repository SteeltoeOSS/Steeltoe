// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public class JsonMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(JsonMessageConverter);

    public const string DefaultClassidFieldName = "__TypeId__";
    public const string DefaultContentClassidFieldName = "__ContentTypeId__";
    public const string DefaultKeyClassidFieldName = "__KeyTypeId__";

    public JsonSerializerSettings Settings { get; set; }

    public override string ServiceName { get; set; } = DefaultServiceName;

    public JsonMessageConverter(ILogger<JsonMessageConverter> logger = null)
        : base(logger)
    {
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false
            }
        };
        Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = contractResolver
        };
    }

    public bool AssumeSupportedContentType { get; set; } = true;

    public MimeType SupportedContentType { get; set; } = MimeTypeUtils.ApplicationJson;

    public Encoding DefaultCharset { get; set; } = EncodingUtils.Utf8;

    public ITypeMapper TypeMapper { get; set; } = new DefaultTypeMapper();

    public TypePrecedence Precedence
    {
        get => TypeMapper.Precedence;
        set => TypeMapper.Precedence = value;
    }

    public override object FromMessage(IMessage message, Type targetType, object conversionHint)
    {
        object content = null;
        var properties = message.Headers;
        if (properties != null)
        {
            var contentType = properties.ContentType();
            if ((AssumeSupportedContentType
                 && (contentType == null || contentType.Equals(RabbitHeaderAccessor.DefaultContentType)))
                || (contentType != null && contentType.Contains(SupportedContentType.Subtype)))
            {
                var encoding = EncodingUtils.GetEncoding(properties.ContentEncoding()) ?? DefaultCharset;

                content = DoFromMessage(message, targetType, conversionHint, properties, encoding);
            }
            else
            {
                Logger?.LogWarning("Could not convert incoming message with content-type ["
                                    + contentType + "], '" + SupportedContentType.Subtype + "' keyword missing.");
            }
        }

        content ??= message.Payload;
        return content;
    }

    protected override IMessage CreateMessage(object objectToConvert, IMessageHeaders headers, object convertionHint)
    {
        byte[] bytes;
        try
        {
            var jsonString = JsonConvert.SerializeObject(objectToConvert, Settings);
            bytes = DefaultCharset.GetBytes(jsonString);
        }
        catch (Exception e)
        {
            throw new MessageConversionException("Failed to convert Message content", e);
        }

        var accessor = RabbitHeaderAccessor.GetMutableAccessor(headers);
        accessor.ContentType = SupportedContentType.ToString();
        accessor.ContentEncoding = EncodingUtils.GetEncoding(DefaultCharset);
        accessor.ContentLength = bytes.Length;
        TypeMapper.FromType(objectToConvert.GetType(), accessor.MessageHeaders);

        var message = Message.Create(bytes, headers);
        return message;
    }

    private object DoFromMessage(IMessage from, Type targetType, object conversionHint, IMessageHeaders headers, Encoding encoding)
    {
        if (from is not IMessage<byte[]> message)
        {
            throw new MessageConversionException($"Failed to convert Message content, message missing byte[] {from.GetType()}");
        }

        object content;
        try
        {
            if (conversionHint is ParameterInfo pinfo)
            {
                content = ConvertBytesToObject(message.Payload, encoding, pinfo.ParameterType);
            }
            else if (targetType != null)
            {
                content = ConvertBytesToObject(message.Payload, encoding, targetType);
            }
            else if (TypeMapper != null)
            {
                var target = TypeMapper.ToType(headers);
                content = ConvertBytesToObject(message.Payload, encoding, target);
            }
            else
            {
                content = message.Payload;
            }
        }
        catch (Exception e)
        {
            throw new MessageConversionException("Failed to convert Message content", e);
        }

        return content;
    }

    private object ConvertBytesToObject(byte[] body, Encoding encoding, Type targetClass)
    {
        var contentAsString = encoding.GetString(body);
        return JsonConvert.DeserializeObject(contentAsString, targetClass, Settings);
    }
}

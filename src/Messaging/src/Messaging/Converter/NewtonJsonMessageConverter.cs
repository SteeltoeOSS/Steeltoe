// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Converter;

public class NewtonJsonMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(NewtonJsonMessageConverter);

    public JsonSerializerSettings Settings { get; }

    public override string ServiceName { get; set; } = DefaultServiceName;

    public NewtonJsonMessageConverter()
        : base(new MimeType("application", "json", Encoding.UTF8))
    {
        Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    public NewtonJsonMessageConverter(params MimeType[] supportedMimeTypes)
        : base(new List<MimeType>(supportedMimeTypes))
    {
        Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    public override bool CanConvertFrom(IMessage message, Type targetClass)
    {
        if (targetClass == null || !SupportsMimeType(message.Headers))
        {
            return false;
        }

        return true;
    }

    public override bool CanConvertTo(object payload, IMessageHeaders headers = null)
    {
        if (!SupportsMimeType(headers))
        {
            return false;
        }

        return true;
    }

    protected internal static Type GetIMessageGenericType(Type type)
    {
        var typeFilter = new TypeFilter((t, _) =>
        {
            Type candidate = t;

            if (candidate.IsConstructedGenericType)
            {
                candidate = candidate.GetGenericTypeDefinition();
            }

            return typeof(IMessage<>) == candidate;
        });

        if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IMessage<>))
        {
            return type.GenericTypeArguments[0];
        }

        if (type == typeof(IMessage<>))
        {
            return null;
        }

        Type[] result = type.FindInterfaces(typeFilter, null);

        if (result.Length > 0)
        {
            return result[0].GenericTypeArguments[0];
        }

        return null;
    }

    protected internal static bool IsIMessageGenericType(Type type)
    {
        return GetIMessageGenericType(type) != null;
    }

    protected internal static Type GetTargetType(Type targetClass, object conversionHint)
    {
        if (conversionHint is ParameterInfo info)
        {
            Type paramType = info.ParameterType;
            Type messageType = GetIMessageGenericType(paramType);

            if (messageType != null)
            {
                return messageType;
            }

            return paramType;
        }

        return targetClass;
    }

    protected static Encoding GetJsonEncoding(MimeType contentType)
    {
        if (contentType != null && contentType.Encoding != null)
        {
            return contentType.Encoding;
        }

        return EncodingUtils.Utf8;
    }

    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        Type target = GetTargetType(targetClass, conversionHint);
        object payload = message.Payload;

        if (targetClass.IsInstanceOfType(payload))
        {
            return payload;
        }

        var serializer = JsonSerializer.Create(Settings);

        try
        {
            TextReader textReader = null;

            if (payload is byte[] payloadBytes)
            {
                var buffer = new MemoryStream(payloadBytes, false);
                textReader = new StreamReader(buffer, true);
            }
            else
            {
                textReader = new StringReader(payload.ToString());
            }

            return serializer.Deserialize(textReader, target);
        }
        catch (Exception ex)
        {
            throw new MessageConversionException(message, $"Could not read JSON: {ex.Message}", ex);
        }
    }

    protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
    {
        var serializer = JsonSerializer.Create(Settings);

        try
        {
            if (typeof(byte[]) == SerializedPayloadClass)
            {
                var memStream = new MemoryStream(1024);
                Encoding encoding = GetJsonEncoding(GetMimeType(headers));

                var writer = new StreamWriter(memStream, encoding)
                {
                    AutoFlush = true
                };

                serializer.Serialize(writer, payload);
                payload = memStream.ToArray();
            }
            else
            {
                var writer = new StringWriter();
                serializer.Serialize(writer, payload);
                payload = writer.ToString();
            }
        }
        catch (Exception ex)
        {
            throw new MessageConversionException($"Could not write JSON: {ex.Message}", ex);
        }

        return payload;
    }

    protected override bool Supports(Type type)
    {
        throw new InvalidOperationException();
    }
}

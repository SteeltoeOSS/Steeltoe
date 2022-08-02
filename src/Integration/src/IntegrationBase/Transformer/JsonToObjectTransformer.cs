// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Integration.Transformer;

public class JsonToObjectTransformer : AbstractTransformer
{
    private readonly DefaultTypeMapper _defaultTypeMapper = new();

    public Type TargetType { get; set; }

    public JsonSerializerSettings Settings { get; set; }

    public Encoding DefaultCharset { get; set; }

    public JsonToObjectTransformer(IApplicationContext context, Type targetType = null)
        : base(context)
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

        TargetType = targetType;
        _defaultTypeMapper.DefaultType = null;
        DefaultCharset = EncodingUtils.Utf8;
    }

    protected override object DoTransform(IMessage message)
    {
        IMessageHeaders headers = message.Headers;
        bool removeHeaders = false;
        Type targetClass = ObtainResolvableTypeFromHeadersIfAny(headers);

        if (targetClass == null)
        {
            targetClass = TargetType;
        }
        else
        {
            removeHeaders = true;
        }

        object payload = message.Payload;
        object result;

        switch (payload)
        {
            case string sPayload:
                result = JsonConvert.DeserializeObject(sPayload, targetClass, Settings);
                break;
            case byte[] bPayload:
            {
                string contentAsString = DefaultCharset.GetString(bPayload);
                result = JsonConvert.DeserializeObject(contentAsString, targetClass, Settings);
                break;
            }

            default:
                throw new MessageConversionException($"Failed to convert Message content, message missing byte[] or string: {payload.GetType()}");
        }

        if (removeHeaders)
        {
            return MessageBuilderFactory.WithPayload(result).CopyHeaders(headers)
                .RemoveHeaders(MessageHeaders.TypeId, MessageHeaders.ContentTypeId, MessageHeaders.KeyTypeId).Build();
        }

        return result;
    }

    private Type ObtainResolvableTypeFromHeadersIfAny(IMessageHeaders headers)
    {
        return _defaultTypeMapper.ToType(headers);
    }
}

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
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Transformer;

public class ObjectToJsonTransformer : AbstractTransformer
{
    private readonly DefaultTypeMapper _defaultTypeMapper = new();

    public Type ResultType { get; set; }

    public JsonSerializerSettings Settings { get; set; }

    public string ContentType { get; set; }

    public Encoding DefaultCharset { get; set; }

    public ObjectToJsonTransformer(IApplicationContext context, Type resultType = null)
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

        ResultType = resultType ?? typeof(string);
        DefaultCharset = EncodingUtils.Utf8;
        ContentType = MessageHeaders.ContentTypeJson;
    }

    protected override object DoTransform(IMessage message)
    {
        object payload = BuildJsonPayload(message.Payload);
        MessageHeaderAccessor accessor = MessageHeaderAccessor.GetMutableAccessor(message);
        string contentType = accessor.ContentType;

        if (string.IsNullOrEmpty(contentType))
        {
            accessor.ContentType = ContentType;
        }

        IMessageHeaders headers = accessor.MessageHeaders;
        _defaultTypeMapper.FromType(message.Payload.GetType(), headers);

        if (ResultType == typeof(string))
        {
            return MessageBuilderFactory.WithPayload((string)payload).CopyHeaders(headers).Build();
        }

        return MessageBuilderFactory.WithPayload((byte[])payload).CopyHeaders(headers).Build();
    }

    private object BuildJsonPayload(object payload)
    {
        string jsonString = JsonConvert.SerializeObject(payload, Settings);

        if (ResultType == typeof(string))
        {
            return jsonString;
        }

        if (ResultType == typeof(byte[]))
        {
            return DefaultCharset.GetBytes(jsonString);
        }

        throw new InvalidOperationException($"Unsupported result type: {ResultType}");
    }
}

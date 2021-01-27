// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;
using System.Text;

namespace Steeltoe.Integration.Transformer
{
    public class JsonToObjectTransformer : AbstractTransformer
    {
        private DefaultTypeMapper _defaultTypeMapper = new DefaultTypeMapper();

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
            Settings = new JsonSerializerSettings()
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
            var headers = message.Headers;
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

            var payload = message.Payload;
            object result = null;

            if (payload is string)
            {
                result = JsonConvert.DeserializeObject((string)payload, targetClass, Settings);
            }
            else if (payload is byte[])
            {
                var contentAsString = DefaultCharset.GetString((byte[])payload);
                result = JsonConvert.DeserializeObject(contentAsString, targetClass, Settings);
            }
            else
            {
                throw new MessageConversionException("Failed to convert Message content, message missing byte[] or string: " + payload.GetType());
            }

            if (removeHeaders)
            {
                return MessageBuilderFactory
                    .WithPayload(result)
                    .CopyHeaders(headers)
                    .RemoveHeaders(MessageHeaders.TYPE_ID, MessageHeaders.CONTENT_TYPE_ID, MessageHeaders.KEY_TYPE_ID)
                    .Build();
            }

            return result;
        }

        private Type ObtainResolvableTypeFromHeadersIfAny(IMessageHeaders headers)
        {
            return _defaultTypeMapper.ToType(headers);
        }
    }
}

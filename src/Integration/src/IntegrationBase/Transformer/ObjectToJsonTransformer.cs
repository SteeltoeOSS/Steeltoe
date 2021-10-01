// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Text;

namespace Steeltoe.Integration.Transformer
{
    public class ObjectToJsonTransformer : AbstractTransformer
    {
        private DefaultTypeMapper _defaultTypeMapper = new ();

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
            Settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = contractResolver
            };

            ResultType = resultType ?? typeof(string);
            DefaultCharset = EncodingUtils.Utf8;
            ContentType = MessageHeaders.CONTENT_TYPE_JSON;
        }

        protected override object DoTransform(IMessage message)
        {
            var payload = BuildJsonPayload(message.Payload);
            var accessor = MessageHeaderAccessor.GetMutableAccessor(message);
            var contentType = accessor.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                accessor.ContentType = ContentType;
            }

            var headers = accessor.MessageHeaders;
            _defaultTypeMapper.FromType(message.Payload.GetType(), headers);
            if (ResultType == typeof(string))
            {
                return MessageBuilderFactory.WithPayload<string>((string)payload).CopyHeaders(headers).Build();
            }
            else
            {
                return MessageBuilderFactory.WithPayload<byte[]>((byte[])payload).CopyHeaders(headers).Build();
            }
        }

        private object BuildJsonPayload(object payload)
        {
            var jsonString = JsonConvert.SerializeObject(payload, Settings);

            if (ResultType == typeof(string))
            {
                return jsonString;
            }
            else if (ResultType == typeof(byte[]))
            {
                return DefaultCharset.GetBytes(jsonString);
            }
            else
            {
                throw new InvalidOperationException("Unsupported result type: " + ResultType);
            }
        }
    }
}
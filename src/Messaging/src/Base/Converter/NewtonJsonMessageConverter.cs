// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.Converter
{
    public class NewtonJsonMessageConverter : AbstractMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(NewtonJsonMessageConverter);

        public JsonSerializerSettings Settings { get; }

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public NewtonJsonMessageConverter()
        : base(new MimeType("application", "json", Encoding.UTF8))
        {
            Settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public NewtonJsonMessageConverter(params MimeType[] supportedMimeTypes)
        : base(new List<MimeType>(supportedMimeTypes))
        {
            Settings = new JsonSerializerSettings()
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

        protected internal bool IsIMessageGenericType(Type type)
        {
            return GetIMessageGenericType(type) != null;
        }

        protected internal Type GetIMessageGenericType(Type type)
        {
            var typeFilter = new TypeFilter((t, c) =>
            {
                var candidate = t;
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

            var result = type.FindInterfaces(typeFilter, null);
            if (result.Length > 0)
            {
                return result[0].GenericTypeArguments[0];
            }

            return null;
        }

        protected internal Type GetTargetType(Type targetClass, object conversionHint)
        {
            if (conversionHint is ParameterInfo info)
            {
                var paramType = info.ParameterType;
                var messageType = GetIMessageGenericType(paramType);
                if (messageType != null)
                {
                    return messageType;
                }

                return paramType;
            }

            return targetClass;
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            var target = GetTargetType(targetClass, conversionHint);
            var payload = message.Payload;
            if (targetClass.IsInstanceOfType(payload))
            {
                return payload;
            }

            var serializer = JsonSerializer.Create(Settings);
            try
            {
                TextReader textReader = null;
                if (payload is byte[])
                {
                    var buffer = new MemoryStream((byte[])payload, false);
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
                throw new MessageConversionException(message, "Could not read JSON: " + ex.Message, ex);
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
                    var encoding = GetJsonEncoding(GetMimeType(headers));
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
                throw new MessageConversionException("Could not write JSON: " + ex.Message, ex);
            }

            return payload;
        }

        protected override bool Supports(Type clazz)
        {
            throw new InvalidOperationException();
        }

        protected Encoding GetJsonEncoding(MimeType contentType)
        {
            if (contentType != null && (contentType.Encoding != null))
            {
                return contentType.Encoding;
            }

            return EncodingUtils.Utf8;
        }
    }
}

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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Data;
using System;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class JsonMessageConverter : AbstractMessageConverter, ISmartMessageConverter
    {
        public const string DEFAULT_CLASSID_FIELD_NAME = "__TypeId__";
        public const string DEFAULT_CONTENT_CLASSID_FIELD_NAME = "__ContentTypeId__";
        public const string DEFAULT_KEY_CLASSID_FIELD_NAME = "__KeyTypeId__";

        private readonly ILogger _logger;

        private JsonSerializerSettings Settings { get; }

        public JsonMessageConverter(ILogger logger = null)
        {
            _logger = logger;
            Settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public bool AssumeSupportedContentType { get; set; } = true;

        public MimeType SupportedContentType { get; set; } = MimeTypeUtils.APPLICATION_JSON;

        public Encoding DefaultCharset { get; set; } = EncodingUtils.Utf8;

        public override object FromMessage(Message message)
        {
            return FromMessage(message, null);
        }

        public object FromMessage(Message message, object conversionHint)
        {
            object content = null;
            var properties = message.MessageProperties;
            if (properties != null)
            {
                var contentType = properties.ContentType;
                if ((AssumeSupportedContentType
                    && (contentType == null || contentType.Equals(MessageProperties.DEFAULT_CONTENT_TYPE)))
                    || (contentType != null && contentType.Contains(SupportedContentType.Subtype)))
                {
                    var encoding = EncodingUtils.GetEncoding(properties.ContentEncoding);
                    if (encoding == null)
                    {
                        encoding = DefaultCharset;
                    }

                    content = DoFromMessage(message, conversionHint, properties, encoding);
                }
                else
                {
                    _logger?.LogWarning("Could not convert incoming message with content-type ["
                            + contentType + "], '" + SupportedContentType.Subtype + "' keyword missing.");
                }
            }

            if (content == null)
            {
                content = message.Body;
            }

            return content;
        }

        protected override Message CreateMessage(object payload, MessageProperties messageProperties)
        {
            return CreateMessage(payload, messageProperties, null);
        }

        protected override Message CreateMessage(object objectToConvert, MessageProperties messageProperties, Type genericType)
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

            messageProperties.ContentType = SupportedContentType.ToString();
            messageProperties.ContentEncoding = EncodingUtils.GetEncoding(DefaultCharset);
            messageProperties.ContentLength = bytes.Length;

            // var type =  genericType == null ? objectToConvert.GetType() : genericType;
            // if (genericType != null && !type.isContainerType()
            //        && Modifier.isAbstract(type.getRawClass().getModifiers()))
            // {
            //    type = this.objectMapper.constructType(objectToConvert.getClass());
            // }
            // FromType(type, messageProperties);
            return new Message(bytes, messageProperties);
        }

        protected string RetrieveHeaderAsString(MessageProperties properties, string headerName)
        {
            properties.Headers.TryGetValue(headerName, out var result);
            string resultString = null;
            if (result != null)
            {
                resultString = result.ToString();
            }

            return resultString;
        }

        protected string RetrieveHeader(MessageProperties properties, string headerName)
        {
            var classId = RetrieveHeaderAsString(properties, headerName);
            if (classId == null)
            {
                throw new MessageConversionException(
                        "failed to convert Message content. Could not resolve " + headerName + " in header");
            }

            return classId;
        }

        private object DoFromMessage(Message message, object conversionHint, MessageProperties properties, Encoding encoding)
        {
            object content;
            try
            {
                if (conversionHint is Type)
                {
                    content = ConvertBytesToObject(message.Body, encoding, (Type)conversionHint);
                }
                else
                {
                    var targetType = ToType(message.MessageProperties);
                    content = ConvertBytesToObject(message.Body, encoding, targetType);
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

        private Type ToType(MessageProperties properties)
        {
            var inferredType = properties.InferredArgumentType;
            if (inferredType != null && !inferredType.IsAbstract && !inferredType.IsInterface)
            {
                return inferredType;
            }

            var typeIdHeader = RetrieveHeaderAsString(properties, DEFAULT_CLASSID_FIELD_NAME);

            if (typeIdHeader != null)
            {
                return FromTypeHeader(properties, typeIdHeader);
            }

            return typeof(object);
        }

        private Type FromTypeHeader(MessageProperties properties, string typeIdHeader)
        {
            var classType = GetClassIdType(typeIdHeader);
            if (classType != null)
            {
                return classType;
            }

            return typeof(object);

            // var contentClassType = GetClassIdType(RetrieveHeader(properties, DEFAULT_CONTENT_CLASSID_FIELD_NAME));
            // if (classType.getKeyType() == null)
            // {
            //    return TypeFactory.defaultInstance()
            //            .constructCollectionLikeType(classType.getRawClass(), contentClassType);
            // }

            // JavaType keyClassType = getClassIdType(retrieveHeader(properties, getKeyClassIdFieldName()));
            // return TypeFactory.defaultInstance()
            //        .constructMapLikeType(classType.getRawClass(), keyClassType, contentClassType);
        }

        private Type GetClassIdType(string typeIdHeader)
        {
            try
            {
                return Type.GetType(typeIdHeader, false);
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception during GetClassIdType()", e);
            }

            return null;
        }
    }
}

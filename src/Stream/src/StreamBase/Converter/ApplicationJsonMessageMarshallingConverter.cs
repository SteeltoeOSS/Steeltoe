// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Converter
{
    public class ApplicationJsonMessageMarshallingConverter : NewtonJsonMessageConverter
    {
        internal ApplicationJsonMessageMarshallingConverter()
        {
        }

        protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            if (payload is byte[])
            {
                return payload;
            }
            else if (payload is string)
            {
                return EncodingUtils.Utf8.GetBytes((string)payload);
            }
            else
            {
                return base.ConvertToInternal(payload, headers, conversionHint);
            }
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            object result = null;
            if (conversionHint is ParameterInfo)
            {
                var conversionHintType = ((ParameterInfo)conversionHint).ParameterType;
                if (IsIMessageGenericType(conversionHintType))
                {
                    /*
                     * Ensures that super won't attempt to create Message as a result of
                     * conversion and stays at payload conversion only. The Message will
                     * eventually be created in
                     * MessageMethodArgumentResolver.resolveArgument(..)
                     */
                    conversionHint = null;
                }

                // TODO: Look at Java code that uses  (ConvertParameterizedType) below
            }

            // if (result == null)
            // {
            if (message.Payload is byte[] && targetClass.IsAssignableFrom(typeof(string)))
            {
                result = EncodingUtils.Utf8.GetString((byte[])message.Payload);
            }
            else
            {
                result = base.ConvertFromInternal(message, targetClass, conversionHint);
            }

            // }
            return result;
        }

        // private object ConvertParameterizedType(IMessage message, Type targetClass, ParameterizedTypeReference conversionHint)
        // {
        //    ObjectMapper objectMapper = this.getObjectMapper();
        //    object payload = message.Payload;
        //    try
        //    {
        //        JavaType type = this.typeCache.get(conversionHint);

        // if (type == null)
        //        {
        //            type = objectMapper.getTypeFactory().constructType((conversionHint).getType());
        //            this.typeCache.put(conversionHint, type);
        //        }

        // if (payload is byte[])
        //        {
        //            return objectMapper.readValue((byte[])payload, type);
        //        }

        // else if (payload is String)
        //        {
        //            return objectMapper.readValue((String)payload, type);
        //        }

        // else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (IOException e)
        //    {
        //        throw new MessageConversionException("Cannot parse payload ", e);
        //    }
        // }
    }
}

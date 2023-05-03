// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Stream.Converter;

public class ApplicationJsonMessageMarshallingConverter : NewtonJsonMessageConverter
{
    internal ApplicationJsonMessageMarshallingConverter()
    {
    }

    protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
    {
        return payload switch
        {
            byte[] => payload,
            string sPayload => EncodingUtils.Utf8.GetBytes(sPayload),
            _ => base.ConvertToInternal(payload, headers, conversionHint)
        };
    }

    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        if (conversionHint is ParameterInfo info)
        {
            Type conversionHintType = info.ParameterType;

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
        }

        object result;

        if (message.Payload is byte[] v && targetClass.IsAssignableFrom(typeof(string)))
        {
            result = EncodingUtils.Utf8.GetString(v);
        }
        else
        {
            result = base.ConvertFromInternal(message, targetClass, conversionHint);
        }

        return result;
    }
}

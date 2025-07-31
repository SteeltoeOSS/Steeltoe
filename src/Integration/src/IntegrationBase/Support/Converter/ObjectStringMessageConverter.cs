// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;

namespace Steeltoe.Integration.Support.Converter;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class ObjectStringMessageConverter : StringMessageConverter
{
    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        var payload = message.Payload;
        if (payload is string || payload is byte[])
        {
            return base.ConvertFromInternal(message, targetClass, conversionHint);
        }
        else
        {
            return payload.ToString();
        }
    }
}
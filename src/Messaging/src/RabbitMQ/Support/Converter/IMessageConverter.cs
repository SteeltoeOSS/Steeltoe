// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Data;
using System;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public interface IMessageConverter
    {
        Message ToMessage(object payload, MessageProperties messageProperties);

        Message ToMessage(object payload, MessageProperties messageProperties, Type genericType)
        {
            return ToMessage(payload, messageProperties);
        }

        object FromMessage(Message message);
    }
}

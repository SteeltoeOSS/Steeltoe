// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using System;

namespace Steeltoe.Stream.Converter;

public class ObjectSupportingByteArrayMessageConverter : ByteArrayMessageConverter
{
    protected override bool Supports(Type clazz)
    {
        if (!base.Supports(clazz))
        {
            return typeof(object) == clazz;
        }

        return true;
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Attributes;
using Steeltoe.Messaging;
using Steeltoe.Stream.Messaging;
using System;

namespace Steeltoe.Stream.Tck
{
    public class TypelessMessageConfigurationSA
    {
        [ServiceActivator(InputChannel = IProcessor.INPUT, OutputChannel = IProcessor.OUTPUT)]
        public object Echo(IMessage value)
        {
            Console.WriteLine(value.Payload);
            return value.Payload;
        }
    }
}

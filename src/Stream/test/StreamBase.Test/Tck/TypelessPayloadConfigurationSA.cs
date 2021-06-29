// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Attributes;
using Steeltoe.Stream.Messaging;
using System;

namespace Steeltoe.Stream.Tck
{
    public class TypelessPayloadConfigurationSA
    {
        [ServiceActivator(InputChannel = IProcessor.INPUT, OutputChannel = IProcessor.OUTPUT)]
        public object Echo(object value)
        {
            Console.WriteLine(value);
            return value;
        }
    }
}

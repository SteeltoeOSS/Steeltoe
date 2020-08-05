// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Tck
{
    public class InternalPipeLine
    {
        [StreamListener(IProcessor.INPUT)]
        [SendTo("internalchannel")]
        public string HandleA(Person value)
        {
            return "{\"name\":\"" + value.Name.ToUpper() + "\"}";
        }

        [StreamListener("internalchannel")]
        [SendTo(IProcessor.OUTPUT)]
        public string HandleB(Person value)
        {
            return value.ToString();
        }
    }
}

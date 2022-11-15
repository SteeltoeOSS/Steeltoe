// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Test.Tck;

public class MessageHeadersInputConfiguration
{
    [StreamListener(ISink.InputName)]
    [SendTo(ISource.OutputName)]
    public IDictionary<string, object> Echo(MessageHeaders value)
    {
        return value;
    }
}

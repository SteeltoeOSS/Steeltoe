// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.TestBinder;

public class InputDestination : AbstractDestination
{
    public void Send(IMessage<byte[]> message)
    {
        Channel.Send(message);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.TestBinder;

public class AbstractDestination
{
    private ISubscribableChannel _channel;

    internal ISubscribableChannel Channel
    {
        get => _channel;

        set
        {
            _channel = value;
            AfterChannelIsSet();
        }
    }

    protected internal virtual void AfterChannelIsSet()
    {
    }
}

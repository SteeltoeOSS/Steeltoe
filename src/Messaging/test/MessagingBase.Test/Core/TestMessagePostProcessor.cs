// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Core.Test;

internal class TestMessagePostProcessor : IMessagePostProcessor
{
    private IMessage message;

    public IMessage Message
    {
        get { return message; }
    }

    public IMessage PostProcessMessage(IMessage message)
    {
        this.message = message;
        return message;
    }
}
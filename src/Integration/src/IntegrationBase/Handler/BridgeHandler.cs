// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

public class BridgeHandler : AbstractReplyProducingMessageHandler
{
    public BridgeHandler(IApplicationContext context)
        : base(context)
    {
    }

    public override string ComponentType
    {
        get { return "bridge"; }
    }

    public override void Initialize()
    {
        // Nothing to do
    }

    protected override object HandleRequestMessage(IMessage requestMessage)
    {
        return requestMessage;
    }

    protected override bool ShouldCopyRequestHeaders
    {
        get { return false; }
    }
}
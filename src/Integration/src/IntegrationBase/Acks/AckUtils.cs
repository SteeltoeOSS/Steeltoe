// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Integration.Acks;

public static class AckUtils
{
    public static void AutoAck(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null && ackCallback.IsAutoAck && !ackCallback.IsAcknowledged)
        {
            ackCallback.Acknowledge(Status.ACCEPT);
        }
    }

    public static void AutoNack(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null && ackCallback.IsAutoAck && !ackCallback.IsAcknowledged)
        {
            ackCallback.Acknowledge(Status.REJECT);
        }
    }

    public static void Accept(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.ACCEPT);
        }
    }

    public static void Reject(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.REJECT);
        }
    }

    public static void Requeue(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.REQUEUE);
        }
    }
}

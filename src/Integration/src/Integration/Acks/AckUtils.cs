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
            ackCallback.Acknowledge(Status.Accept);
        }
    }

    public static void AutoNack(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null && ackCallback.IsAutoAck && !ackCallback.IsAcknowledged)
        {
            ackCallback.Acknowledge(Status.Reject);
        }
    }

    public static void Accept(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.Accept);
        }
    }

    public static void Reject(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.Reject);
        }
    }

    public static void Requeue(IAcknowledgmentCallback ackCallback)
    {
        if (ackCallback != null)
        {
            ackCallback.Acknowledge(Status.Requeue);
        }
    }
}

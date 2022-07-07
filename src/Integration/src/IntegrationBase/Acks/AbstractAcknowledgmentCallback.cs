// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Integration.Acks;

public abstract class AbstractAcknowledgmentCallback : IAcknowledgmentCallback
{
    public virtual bool IsAcknowledged { get; set; }

    public virtual bool IsAutoAck { get => true; set => throw new InvalidOperationException("You cannot disable auto acknowledgment with this implementation"); }

    public abstract void Acknowledge(Status status);
}

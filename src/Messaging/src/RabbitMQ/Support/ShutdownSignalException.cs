// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class ShutdownSignalException : Exception
{
    public ShutdownSignalException(RC.ShutdownEventArgs args)
    {
        Args = args;
    }

    public ushort ClassId => Args.ClassId;

    public ushort MethodId => Args.MethodId;

    public ushort ReplyCode => Args.ReplyCode;

    public string ReplyText => Args.ReplyText;

    public RC.ShutdownInitiator Initiator => Args.Initiator;

    public object Cause => Args.Cause;

    public RC.ShutdownEventArgs Args { get; }

    public override string ToString()
    {
        return Args.ToString();
    }
}

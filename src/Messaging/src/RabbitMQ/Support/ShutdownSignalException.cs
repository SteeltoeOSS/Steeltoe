// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using System;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class ShutdownSignalException : Exception
    {
        public ShutdownSignalException(ShutdownEventArgs args)
        {
            ClassId = args.ClassId;
            MethodId = args.MethodId;
            ReplyCode = args.ReplyCode;
            ReplyText = args.ReplyText;
            Initiator = args.Initiator;
            Cause = args.Cause;
        }

        public ushort ClassId { get; }

        public ushort MethodId { get; }

        public ushort ReplyCode { get; }

        public string ReplyText { get; }

        public ShutdownInitiator Initiator { get; }

        public object Cause { get; }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support
{
    public class ShutdownSignalException : Exception
    {
        private RC.ShutdownEventArgs _args;

        public ShutdownSignalException(RC.ShutdownEventArgs args)
        {
            _args = args;
        }

        public ushort ClassId => _args.ClassId;

        public ushort MethodId => _args.MethodId;

        public ushort ReplyCode => _args.ReplyCode;

        public string ReplyText => _args.ReplyText;

        public RC.ShutdownInitiator Initiator => _args.Initiator;

        public object Cause => _args.Cause;

        public RC.ShutdownEventArgs Args => _args;

        public override string ToString()
        {
            return _args.ToString();
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Data;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CorrelationData // : Correlation
    {
        public CorrelationData(string id)
        {
            Id = id;
            FutureSource = new TaskCompletionSource<Confirm>();
        }

        public string Id { get; set; }

        public Message ReturnedMessage { get; set; }

        public TaskCompletionSource<Confirm> FutureSource { get; }

        public Task<Confirm> Future
        {
            get
            {
                return FutureSource.Task;
            }
        }

        public override string ToString()
        {
            return "CorrelationData [id=" + Id + "]";
        }

        public class Confirm
        {
            public Confirm(bool ack, string reason)
            {
                Ack = ack;
                Reason = reason;
            }

            public bool Ack { get; }

            public string Reason { get; }

            public override string ToString()
            {
                return "Confirm [ack=" + Ack + (Reason != null ? ", reason=" + Reason : string.Empty) + "]";
            }
        }
    }
}

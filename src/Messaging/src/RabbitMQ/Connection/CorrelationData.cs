// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public virtual string Id { get; set; }

        public virtual IMessage ReturnedMessage { get; set; }

        public virtual TaskCompletionSource<Confirm> FutureSource { get; }

        public virtual Task<Confirm> Future
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

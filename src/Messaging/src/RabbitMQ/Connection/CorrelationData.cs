// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class CorrelationData // : Correlation
{
    public virtual string Id { get; set; }

    public virtual IMessage ReturnedMessage { get; set; }

    public virtual TaskCompletionSource<Confirm> FutureSource { get; }

    public virtual Task<Confirm> Future => FutureSource.Task;

    public CorrelationData(string id)
    {
        Id = id;
        FutureSource = new TaskCompletionSource<Confirm>();
    }

    public override string ToString()
    {
        return $"CorrelationData [id={Id}]";
    }

    public class Confirm
    {
        public bool Ack { get; }

        public string Reason { get; }

        public Confirm(bool ack, string reason)
        {
            Ack = ack;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"Confirm [ack={Ack}{(Reason != null ? $", reason={Reason}" : string.Empty)}]";
        }
    }
}

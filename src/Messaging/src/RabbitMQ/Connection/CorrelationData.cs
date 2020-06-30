// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        public IMessage ReturnedMessage { get; set; }

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

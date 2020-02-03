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

namespace Steeltoe.Integration.Acks
{
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
}

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
    /// <summary>
    /// AcknowledgmentCallback status values
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Mark message as accepted
        /// </summary>
        ACCEPT,

        /// <summary>
        /// Mark message as rejected
        /// </summary>
        REJECT,

        /// <summary>
        /// Reject message and requeue
        /// </summary>
        REQUEUE
    }

    /// <summary>
    /// General abstraction over acknowlegements
    /// </summary>
    public interface IAcknowledgmentCallback
    {
        /// <summary>
        /// Acknowledge the message.
        /// </summary>
        /// <param name="status">true if the message is already acked</param>
        void Acknowledge(Status status);

        /// <summary>
        /// Gets or sets a value indicating whether the ack has been
        /// processed by the user so that the framework can auto-ack if needed.
        /// </summary>
        bool IsAcknowledged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether return true if this acknowledgment supports auto ack when it has not been
        /// already ack'd by the application.
        /// </summary>
        bool IsAutoAck { get; set; }
    }
}

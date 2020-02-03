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

namespace Steeltoe.Messaging
{
    /// <summary>
    /// Simple contract for handling a Message
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handle the given method
        /// </summary>
        /// <param name="message">the message to process</param>
        void HandleMessage(IMessage message);
    }
}

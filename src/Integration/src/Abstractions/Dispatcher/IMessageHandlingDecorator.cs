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

using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Dispatcher
{
    /// <summary>
    /// The strategy to decorate message handling tasks.
    /// </summary>
    public interface IMessageHandlingDecorator
    {
        /// <summary>
        /// Decorate the incoming message handling runnable (task)
        /// </summary>
        /// <param name="messageHandlingRunnable">incoming message handling task</param>
        /// <returns>the newly decorated task</returns>
        IMessageHandlingRunnable Decorate(IMessageHandlingRunnable messageHandlingRunnable);
    }
}

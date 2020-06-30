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

using Steeltoe.Common.Retry;
using Steeltoe.Messaging.Rabbit.Core;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public static class SendRetryContextAccessor
    {
        public const string MESSAGE = "message";
        public const string ADDRESS = "address";

        public static IMessage GetMessage(IRetryContext context)
        {
            return (IMessage)context.GetAttribute(MESSAGE);
        }

        public static Address GetAddress(IRetryContext context)
        {
            return (Address)context.GetAttribute(ADDRESS);
        }
    }
}

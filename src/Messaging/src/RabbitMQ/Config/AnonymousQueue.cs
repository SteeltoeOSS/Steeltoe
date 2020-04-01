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

using Steeltoe.Messaging.Rabbit.Core;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class AnonymousQueue : Queue
    {
        public AnonymousQueue(string name)
            : base(name, false, true, true, null)
        {
        }

        public AnonymousQueue(Dictionary<string, object> arguments)
            : this(Base64UrlNamingStrategy.DEFAULT, arguments)
        {
        }

        public AnonymousQueue(INamingStrategy namingStrategy)
            : this(namingStrategy, null)
        {
        }

        public AnonymousQueue(INamingStrategy namingStrategy, Dictionary<string, object> arguments)
            : base(namingStrategy.GenerateName(), false, true, true, arguments)
        {
            if (!Arguments.ContainsKey(Queue.X_QUEUE_MASTER_LOCATOR))
            {
                MasterLocator = "client-local";
            }
        }
    }
}

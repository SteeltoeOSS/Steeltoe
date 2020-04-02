﻿// Copyright 2017 the original author or authors.
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

using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class HeadersExchange : AbstractExchange
    {
        public HeadersExchange(string name)
        : base(name)
        {
        }

        public HeadersExchange(string name, bool durable, bool autoDelete)
        : base(name, durable, autoDelete)
        {
        }

        public HeadersExchange(string name, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(name, durable, autoDelete, arguments)
        {
        }

        public override string Type { get; } = ExchangeTypes.HEADERS;
    }
}

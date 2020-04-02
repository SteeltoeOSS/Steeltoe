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

using Steeltoe.Common.Services;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public abstract class AbstractExchange : AbstractDeclarable, IExchange, IServiceNameAware
    {
        protected AbstractExchange(string name)
            : this(name, true, false)
        {
        }

        protected AbstractExchange(string name, bool durable, bool autoDelete)
         : this(name, durable, autoDelete, null)
        {
        }

        protected AbstractExchange(string name, bool durable, bool autoDelete, Dictionary<string, object> arguments)
            : base(arguments)
        {
            Name = name;
            IsDurable = durable;
            IsAutoDelete = autoDelete;
        }

        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsAutoDelete { get; set; }

        public bool IsDelayed { get; set; }

        public bool IsInternal { get; set; }

        public abstract string Type { get; }

        public override string ToString()
        {
            return "Exchange [name=" + Name +
                            ", type=" + Type +
                            ", durable=" + IsDurable +
                            ", autoDelete=" + IsAutoDelete +
                            ", internal=" + IsInternal +
                            ", arguments=" + Arguments + "]";
        }
    }
}

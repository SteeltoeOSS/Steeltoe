// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

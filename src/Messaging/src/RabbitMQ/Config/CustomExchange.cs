// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class CustomExchange : AbstractExchange
    {
        public CustomExchange(string name, string type)
        : base(name)
        {
            Type = type;
        }

        public CustomExchange(string name, string type, bool durable, bool autoDelete)
        : base(name, durable, autoDelete)
        {
            Type = type;
        }

        public CustomExchange(string name, string type, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(name, durable, autoDelete, arguments)
        {
            Type = type;
        }

        public override string Type { get; }
    }
}

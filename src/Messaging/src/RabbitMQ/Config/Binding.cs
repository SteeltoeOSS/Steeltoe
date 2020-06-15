// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class Binding : AbstractDeclarable, IServiceNameAware
    {
        public enum DestinationType
        {
            QUEUE,
            EXCHANGE
        }

        public Binding(string name, string destination, DestinationType destinationType, string exchange, string routingKey, Dictionary<string, object> arguments)
            : base(arguments)
        {
            Name = name;
            Destination = destination;
            Type = destinationType;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public string Destination { get; set; }

        public string Exchange { get; set; }

        public string RoutingKey { get; set; }

        public DestinationType Type { get; set; }

        public bool IsDestinationQueue => Type == DestinationType.QUEUE;

        public string Name { get; set; }

        public override string ToString()
        {
            return "Binding [destination=" + Destination + ", exchange=" + Exchange + ", routingKey="
                        + RoutingKey + ", arguments=" + Arguments + "]";
        }
    }
}

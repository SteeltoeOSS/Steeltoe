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

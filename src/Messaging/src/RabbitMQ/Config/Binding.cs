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
    public class Binding : AbstractDeclarable, IServiceNameAware, IBinding
    {
        internal static IBinding Create(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey, Dictionary<string, object> arguments)
        {
            if (destinationType == DestinationType.EXCHANGE)
            {
                return new ExchangeBinding(bindingName, destination, exchange, routingKey, arguments);
            }

            return new QueueBinding(bindingName, destination, exchange, routingKey, arguments);
        }

        public enum DestinationType
        {
            QUEUE,
            EXCHANGE
        }

        public Binding(string bindingName)
            : base(null)
        {
            BindingName = ServiceName = bindingName;
        }

        public Binding(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey, Dictionary<string, object> arguments)
            : base(arguments)
        {
            BindingName = ServiceName = bindingName;
            Destination = destination;
            Type = destinationType;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public string ServiceName { get; set; }

        public string Destination { get; set; }

        public string Exchange { get; set; }

        public string RoutingKey { get; set; }

        public DestinationType Type { get; set; }

        public bool IsDestinationQueue => Type == DestinationType.QUEUE;

        public string BindingName { get; set; }

        public override string ToString()
        {
            return "Binding [bindingName=" + BindingName + ", destination=" + Destination + ", exchange=" + Exchange + ", routingKey="
                        + RoutingKey + ", arguments=" + Arguments + "]";
        }
    }
}

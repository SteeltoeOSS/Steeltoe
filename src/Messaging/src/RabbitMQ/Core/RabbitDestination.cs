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

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitDestination
    {
        private string _item1;
        private string _item2;

        public string QueueName
        {
            get
            {
                return _item2;
            }
        }

        public string RoutingKey
        {
            get
            {
                return _item2;
            }
        }

        public string ExchangeName
        {
            get
            {
                return _item1;
            }
        }

        public RabbitDestination(string queueName)
        {
            _item2 = queueName;
        }

        public RabbitDestination(string exchangeName, string routingKey)
        {
            _item1 = exchangeName;
            _item2 = routingKey;
        }

        public static implicit operator string(RabbitDestination destination)
        {
            if (string.IsNullOrEmpty(destination._item1))
            {
                return destination._item2;
            }

            return destination._item1 + "/" + destination._item2;
        }

        public static implicit operator RabbitDestination(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                return new RabbitDestination(string.Empty, string.Empty);
            }

            var result = ParseDestination(destination);
            return new RabbitDestination(result.Item1, result.Item2);
        }

        private static (string, string) ParseDestination(string destination)
        {
            var split = destination.Split('/');
            if (split.Length >= 2)
            {
                return (split[0], split[1]);
            }
            else
            {
                return (string.Empty, split[0]);
            }
        }
    }
}

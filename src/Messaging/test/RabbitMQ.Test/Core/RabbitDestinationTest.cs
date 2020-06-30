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

using Xunit;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitDestinationTest
    {
        [Fact]
        public void TestConvertRabbitDestinationToString()
        {
            var d = new RabbitDestination("foo", "bar");
            var str = d;
            Assert.Equal("foo/bar", str);
            var returned = ReceiveStringReturnDestination(str);
            Assert.Equal("foo", returned.ExchangeName);
            Assert.Equal("bar", returned.RoutingKey);
            Assert.Equal("bar", returned.QueueName);

            d = new RabbitDestination("bar");
            str = d;
            Assert.Equal("bar", str);
            returned = ReceiveString2ReturnDestination(str);
            Assert.Equal(string.Empty, returned.ExchangeName);
            Assert.Equal("bar", returned.RoutingKey);
            Assert.Equal("bar", returned.QueueName);
        }

        [Fact]
        public void TestConvertStringToRabbitDestination()
        {
            RabbitDestination d = "foo/bar";
            Assert.Equal("foo", d.ExchangeName);
            Assert.Equal("bar", d.RoutingKey);
            Assert.Equal("bar", d.QueueName);

            var returned = ReceiveDestinationReturnString(d);
            Assert.Equal("foo/bar", returned);

            d = "bar";
            Assert.Equal(string.Empty, d.ExchangeName);
            Assert.Equal("bar", d.RoutingKey);
            Assert.Equal("bar", d.QueueName);

            returned = ReceiveDestination2ReturnString(d);
            Assert.Equal("bar", returned);

            d = "/bar";
            Assert.Equal(string.Empty, d.ExchangeName);
            Assert.Equal("bar", d.RoutingKey);
            Assert.Equal("bar", d.QueueName);

            returned = ReceiveDestination2ReturnString(d);
            Assert.Equal("bar", returned);
        }

        public RabbitDestination ReceiveStringReturnDestination(string destination)
        {
            Assert.Equal("foo/bar", destination);
            return destination;
        }

        public RabbitDestination ReceiveString2ReturnDestination(string destination)
        {
            Assert.Equal("bar", destination);
            return destination;
        }

        public string ReceiveDestinationReturnString(RabbitDestination destination)
        {
            Assert.Equal("foo/bar", destination);
            return destination;
        }

        public string ReceiveDestination2ReturnString(RabbitDestination destination)
        {
            Assert.Equal("bar", destination);
            return destination;
        }
    }
}

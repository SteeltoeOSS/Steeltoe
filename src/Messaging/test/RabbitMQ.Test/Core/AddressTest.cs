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

using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Support;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class AddressTest
    {
        [Fact]
        public void ToStringCheck()
        {
            Address address = new Address("my-exchange", "routing-key");
            string replyToUri = "my-exchange/routing-key";
            Assert.Equal(replyToUri, address.ToString());
        }

        [Fact]
        public void Parse()
        {
            string replyToUri = "direct://my-exchange/routing-key";
            Address address = new Address(replyToUri);
            Assert.Equal("my-exchange", address.ExchangeName);
            Assert.Equal("routing-key", address.RoutingKey);
        }

        [Fact]
        public void ParseUnstructuredWithRoutingKeyOnly()
        {
            Address address = new Address("my-routing-key");
            Assert.Equal("my-routing-key", address.RoutingKey);
            Assert.Equal("/my-routing-key", address.ToString());

            address = new Address("/foo");
            Assert.Equal("foo", address.RoutingKey);
            Assert.Equal("/foo", address.ToString());

            address = new Address("bar/baz");
            Assert.Equal("bar", address.ExchangeName);
            Assert.Equal("baz", address.RoutingKey);
            Assert.Equal("bar/baz", address.ToString());
        }

        [Fact]
        public void ParseWithoutRoutingKey()
        {
            Address address = new Address("fanout://my-exchange");
            Assert.Equal("my-exchange", address.ExchangeName);
            Assert.Equal(string.Empty, address.RoutingKey);
            Assert.Equal("my-exchange/", address.ToString());
        }

        [Fact]
        public void ParseWithDefaultExchangeAndRoutingKey()
        {
            Address address = new Address("direct:///routing-key");
            Assert.Equal(string.Empty, address.ExchangeName);
            Assert.Equal("routing-key", address.RoutingKey);
            Assert.Equal("/routing-key", address.ToString());
        }

        [Fact]
        public void TestEmpty()
        {
            Address address = new Address("/");
            Assert.Equal(string.Empty, address.ExchangeName);
            Assert.Equal(string.Empty, address.RoutingKey);
            Assert.Equal("/", address.ToString());
        }

        [Fact]
        public void TestDirectReplyTo()
        {
            string replyTo = Address.AMQ_RABBITMQ_REPLY_TO + ".ab/cd/ef";
            var headers = new MessageHeaders();
            var props = RabbitHeaderAccessor.GetMutableAccessor(headers);
            props.ReplyTo = replyTo;
            var message = Message.Create(Encoding.UTF8.GetBytes("foo"), props.MessageHeaders);
            var address = message.Headers.ReplyToAddress();
            Assert.Equal(string.Empty, address.ExchangeName);
            Assert.Equal(replyTo, address.RoutingKey);
            address = props.ReplyToAddress;
            Assert.Equal(string.Empty, address.ExchangeName);
            Assert.Equal(replyTo, address.RoutingKey);
        }

        [Fact]
        public void TestEquals()
        {
            Assert.Equal(new Address("foo/bar"), new Address("foo/bar"));
        }
    }
}

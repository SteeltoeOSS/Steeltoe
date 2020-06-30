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

using Steeltoe.Messaging.Rabbit.Support;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class MessageHeaderTest
    {
        [Fact]
        public void TestReplyTo()
        {
            var properties = new RabbitHeaderAccessor();
            properties.ReplyTo = "foo/bar";
            Assert.Equal("bar", properties.ReplyToAddress.RoutingKey);
        }

        [Fact]
        public void TestReplyToNullByDefault()
        {
            var properties = new RabbitHeaderAccessor();
            Assert.Null(properties.ReplyTo);
            Assert.Null(properties.ReplyToAddress);
        }

        [Fact]
        public void TestDelayHeader()
        {
            var properties = new RabbitHeaderAccessor();
            var delay = 100;
            properties.Delay = delay;
            var headers = properties.ToMessageHeaders();
            Assert.Equal(delay, headers.Get<int>(RabbitHeaderAccessor.X_DELAY));
            properties.Delay = null;
            headers = properties.ToMessageHeaders();
            Assert.False(headers.ContainsKey(RabbitHeaderAccessor.X_DELAY));
        }

        [Fact]
        public void TestContentLengthSet()
        {
            var properties = new RabbitHeaderAccessor();
            properties.ContentLength = 1L;
            Assert.True(properties.IsContentLengthSet);
        }

        [Fact]
        public void TesNoNullPointerInEquals()
        {
            var mp = new RabbitHeaderAccessor();
            mp.LeaveMutable = true;
            var mp2 = new RabbitHeaderAccessor();
            mp2.LeaveMutable = true;
            Assert.True(mp.MessageHeaders.Equals(mp2.MessageHeaders));
        }

        [Fact]
        public void TesNoNullPointerInHashCode()
        {
            var messageList = new HashSet<RabbitHeaderAccessor>();
            messageList.Add(new RabbitHeaderAccessor());
            Assert.Single(messageList);
        }
    }
}

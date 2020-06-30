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

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class MessageTest
    {
        [Fact]
        public void ToStringForEmptyMessageBody()
        {
            var message = Message.Create(new byte[0], new MessageHeaders());
            Assert.NotNull(message.ToString());
        }

        [Fact(Skip = "Standard ContentType header missing")]
        public void ProperEncoding()
        {
            var message = Message.Create(EncodingUtils.Utf16.GetBytes("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP"), new MessageHeaders());
            var acccessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            acccessor.ContentType = MessageHeaders.CONTENT_TYPE_JSON;
            acccessor.ContentEncoding = "UTF-16";
            Assert.Contains("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP", message.ToString());
        }

        [Fact]
        public void ToStringForNullMessageProperties()
        {
            var message = Message.Create(new byte[0], null);
            Assert.NotNull(message.ToString());
        }

        [Fact]
        public void ToStringForNonStringMessageBody()
        {
            var message = Message.Create(default(DateTime), null);
            Assert.NotNull(message.ToString());
        }
    }
}

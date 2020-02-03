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

using Steeltoe.Messaging.Support;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test
{
    public class SimpleMessageConverterTest
    {
        [Fact]
        public void ToMessageWithPayloadAndHeaders()
        {
            var headers = new MessageHeaders(new Dictionary<string, object>() { { "foo", "bar" } });
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage("payload", headers);

            Assert.Equal("payload", message.Payload);
            Assert.Equal("bar", message.Headers["foo"]);
        }

        [Fact]
        public void ToMessageWithPayloadAndMutableHeaders()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;
            var headers = accessor.MessageHeaders;

            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage("payload", headers);

            Assert.Equal("payload", message.Payload);
            Assert.Same(headers, message.Headers);
            Assert.Equal("bar", message.Headers["foo"]);
        }
    }
}

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

using Steeltoe.Messaging.Rabbit.Core;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class QueueNameTest
    {
        [Fact]
        public void TestAnonymous()
        {
            var q = new AnonymousQueue();
            Assert.StartsWith("spring.gen-", q.QueueName);
            q = new AnonymousQueue(new Base64UrlNamingStrategy("foo-"));
            Assert.StartsWith("foo-", q.QueueName);
            q = new AnonymousQueue(GuidNamingStrategy.DEFAULT);
            Assert.Matches("[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}", q.QueueName);
        }
    }
}

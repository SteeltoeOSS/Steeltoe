// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Config
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

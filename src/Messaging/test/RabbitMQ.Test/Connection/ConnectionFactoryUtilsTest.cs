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

using Moq;
using Steeltoe.Common.Transaction;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class ConnectionFactoryUtilsTest
    {
        [Fact]
        public void TestResourceHolder()
        {
            var h1 = new RabbitResourceHolder();
            var h2 = new RabbitResourceHolder();
            var connectionFactory = new Mock<IConnectionFactory>();
            TransactionSynchronizationManager.SetActualTransactionActive(true);
            ConnectionFactoryUtils.BindResourceToTransaction(h1, connectionFactory.Object, true);
            Assert.Same(h1, ConnectionFactoryUtils.BindResourceToTransaction(h2, connectionFactory.Object, true));
            TransactionSynchronizationManager.Clear();
        }
    }
}

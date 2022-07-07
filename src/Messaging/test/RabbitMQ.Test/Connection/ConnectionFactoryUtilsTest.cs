// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Transaction;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

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

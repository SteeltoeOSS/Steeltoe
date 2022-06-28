// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public class DlqExpiryTests : IClassFixture<DlqStartupFixture>
{
    private readonly DlqStartupFixture _fixture;
    private readonly ServiceProvider _provider;

    public DlqExpiryTests(DlqStartupFixture fix)
    {
        _fixture = fix;
        _provider = _fixture.Provider;
    }

    [Fact]
    public void TestExpiredDies()
    {
        var template = _provider.GetRabbitTemplate();
        var listener = _provider.GetService<Listener>();
        var context = _provider.GetApplicationContext();
        var queue1 = context.GetService<IQueue>("test.expiry.main");

        template.ConvertAndSend(queue1.QueueName, "foo");
        Assert.True(listener.Latch.Wait(TimeSpan.FromSeconds(10)));
        Thread.Sleep(300);
        Assert.Equal(2, listener.Counter);
    }
}
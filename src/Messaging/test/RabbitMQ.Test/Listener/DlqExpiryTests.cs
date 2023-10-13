// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Test.Listener;

[Trait("Category", "Integration")]
public sealed class DlqExpiryTests : IClassFixture<DlqStartupFixture>
{
    private readonly ServiceProvider _provider;

    public DlqExpiryTests(DlqStartupFixture fixture)
    {
        _provider = fixture.Provider;
    }

    [Fact]
    public async Task TestExpiredDies()
    {
        RabbitTemplate template = _provider.GetRabbitTemplate();
        var listener = _provider.GetService<Listener>();
        IApplicationContext context = _provider.GetApplicationContext();
        var queue1 = context.GetService<IQueue>("test.expiry.main");

        template.ConvertAndSend(queue1.QueueName, "foo");
        Assert.True(listener.Latch.Wait(TimeSpan.FromSeconds(10)));
        await Task.Delay(300);
        Assert.Equal(2, listener.Counter);
    }
}

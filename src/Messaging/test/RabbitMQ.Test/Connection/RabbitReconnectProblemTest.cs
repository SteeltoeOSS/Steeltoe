// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class RabbitReconnectProblemTest
{
    private readonly ITestOutputHelper _output;

    public RabbitReconnectProblemTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Requires manual intervention")]
    public void SurviveAReconnect()
    {
        var myQueue = new Config.Queue("my-queue");
        var cf = new RC.ConnectionFactory
        {
            Uri = new Uri("amqp://localhost")
        };

        var ccf = new CachingConnectionFactory(cf)
        {
            ChannelCacheSize = 2,
            ChannelCheckoutTimeout = 2000
        };
        var admin = new RabbitAdmin(ccf);
        admin.DeclareQueue(myQueue);
        var template = new RabbitTemplate(ccf);
        CheckIt(template, 0, myQueue.ActualName);

        var i = 1;
        while (i < 45)
        {
            // While in this loop, stop and start the broker
            // The CCF should reconnect and the receives in
            // CheckIt should stop throwing exceptions
            // The available permits should always be == 2.
            Thread.Sleep(2000);
            CheckIt(template, i++, myQueue.ActualName);
            using var values = ccf.CheckoutPermits.Values.GetEnumerator();
            values.MoveNext();
            var availablePermits = values.Current.CurrentCount;
            _output.WriteLine("Permits after test: " + availablePermits);
            Assert.Equal(2, availablePermits);
        }
    }

    private void CheckIt(RabbitTemplate template, int counter, string name)
    {
        try
        {
            _output.WriteLine("#" + counter);
            template.Receive(name);
            _output.WriteLine("Ok");
        }
        catch (Exception e)
        {
            _output.WriteLine("Failed: " + e.Message);
        }
    }
}

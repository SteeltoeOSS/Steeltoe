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

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class RabbitReconnectProblemTest
    {
        private readonly ITestOutputHelper output;

        public RabbitReconnectProblemTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact(Skip = "Requires manual intervention")]
        public void SurviveAReconnect()
        {
            var myQueue = new Config.Queue("my-queue");
            var cf = new ConnectionFactory();
            cf.Uri = new Uri("amqp://localhost");

            var ccf = new CachingConnectionFactory(cf);
            ccf.ChannelCacheSize = 2;
            ccf.ChannelCheckoutTimeout = 2000;
            var admin = new RabbitAdmin(ccf);
            admin.DeclareQueue(myQueue);
            var template = new RabbitTemplate(ccf);
            CheckIt(template, 0, myQueue.ActualName);

            int i = 1;
            while (i < 45)
            {
                // While in this loop, stop and start the broker
                // The CCF should reconnect and the receives in
                // Checkit should stop throwing exceptions
                // The available permits should always be == 2.
                Thread.Sleep(2000);
                CheckIt(template, i++, myQueue.ActualName);
                var values = ccf._checkoutPermits.Values.GetEnumerator();
                values.MoveNext();
                var availablePermits = values.Current.CurrentCount;
                output.WriteLine("Permits after test: " + availablePermits);
                Assert.Equal(2, availablePermits);
            }
        }

        private void CheckIt(RabbitTemplate template, int counter, string name)
        {
            try
            {
                output.WriteLine("#" + counter);
                var message = template.Receive(name);
                output.WriteLine("Ok");
            }
            catch (Exception e)
            {
                output.WriteLine("Failed: " + e.Message);
            }
        }
    }
}

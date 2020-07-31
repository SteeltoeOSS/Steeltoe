// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Services
{
    public class HystrixRabbitMQServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            var uri = "amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66";
            var uris = new List<string>() { "amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66" };

            // string managementUri = "https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/";
            // List<string> managementUris = new List<string>() { "https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/" };
            var isSSLEnabled = false;

            var r1 = new HystrixRabbitMQServiceInfo("myId", uri, isSSLEnabled);
            var r2 = new HystrixRabbitMQServiceInfo("myId", uri, uris, isSSLEnabled);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("amqp", r1.Scheme);
            Assert.Equal("192.168.0.81", r1.Host);
            Assert.Equal(5672, r1.Port);
            Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r1.Password);
            Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r1.UserName);
            Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r1.Path);
            Assert.Null(r1.Query);
            Assert.Null(r1.Uris);
            Assert.Equal(uri, r1.Uri);

            Assert.Equal("myId", r2.Id);
            Assert.Equal("amqp", r2.Scheme);
            Assert.Equal("192.168.0.81", r2.Host);
            Assert.Equal(5672, r2.Port);
            Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r2.Password);
            Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r2.UserName);
            Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r2.Path);
            Assert.Null(r2.Query);
            Assert.Equal(uris[0], r2.Uris[0]);
            Assert.Equal(uri, r2.Uri);
        }
    }
}

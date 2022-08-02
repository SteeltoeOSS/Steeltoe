// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class RabbitMQServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        string uri = "amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66";

        var uris = new List<string>
        {
            "amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66"
        };

        string managementUri = "https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/";

        var managementUris = new List<string>
        {
            "https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/"
        };

        var r1 = new RabbitMQServiceInfo("myId", uri);
        var r2 = new RabbitMQServiceInfo("myId", uri, managementUri);
        var r3 = new RabbitMQServiceInfo("myId", uri, managementUri, uris, managementUris);

        var r4 = new RabbitMQServiceInfo("myId", "192.168.0.81", 5672, "03c7a684-6ff1-4bd0-ad45-d10374ffb2af", "l5oq2q0unl35s6urfsuib0jvpo",
            "fb03d693-91fe-4dc5-8203-ff7a6390df66");

        var r5 = new RabbitMQServiceInfo("myId", "192.168.0.81", 5672, "03c7a684-6ff1-4bd0-ad45-d10374ffb2af", "l5oq2q0unl35s6urfsuib0jvpo",
            "fb03d693-91fe-4dc5-8203-ff7a6390df66",
            "https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/");

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
        Assert.Null(r1.ManagementUris);
        Assert.Null(r1.ManagementUri);

        Assert.Equal("myId", r2.Id);
        Assert.Equal("amqp", r2.Scheme);
        Assert.Equal("192.168.0.81", r2.Host);
        Assert.Equal(5672, r2.Port);
        Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r2.Password);
        Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r2.UserName);
        Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r2.Path);
        Assert.Null(r2.Query);
        Assert.Null(r2.Uris);
        Assert.Equal(uri, r2.Uri);
        Assert.Null(r2.ManagementUris);
        Assert.Equal(managementUri, r2.ManagementUri);

        Assert.Equal("myId", r3.Id);
        Assert.Equal("amqp", r3.Scheme);
        Assert.Equal("192.168.0.81", r3.Host);
        Assert.Equal(5672, r3.Port);
        Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r3.Password);
        Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r3.UserName);
        Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r3.Path);
        Assert.Null(r3.Query);
        Assert.NotNull(r3.Uris);
        Assert.Single(r3.Uris);
        Assert.Equal(uris[0], r3.Uris[0]);
        Assert.Equal(uri, r3.Uri);
        Assert.NotNull(r3.ManagementUris);
        Assert.Single(r3.ManagementUris);
        Assert.Equal(managementUris[0], r3.ManagementUris[0]);
        Assert.Equal(managementUri, r3.ManagementUri);

        Assert.Equal("myId", r4.Id);
        Assert.Equal("amqp", r4.Scheme);
        Assert.Equal("192.168.0.81", r4.Host);
        Assert.Equal(5672, r4.Port);
        Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r4.Password);
        Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r4.UserName);
        Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r4.Path);
        Assert.Null(r4.Query);
        Assert.Null(r4.Uris);
        Assert.Equal(uri, r4.Uri);
        Assert.Null(r4.ManagementUris);
        Assert.Null(r4.ManagementUri);

        Assert.Equal("myId", r5.Id);
        Assert.Equal("amqp", r5.Scheme);
        Assert.Equal("192.168.0.81", r5.Host);
        Assert.Equal(5672, r5.Port);
        Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", r5.Password);
        Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", r5.UserName);
        Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", r5.Path);
        Assert.Null(r5.Query);
        Assert.Null(r5.Uris);
        Assert.Equal(uri, r5.Uri);
        Assert.Null(r5.ManagementUris);
        Assert.Equal(managementUri, r5.ManagementUri);
    }
}

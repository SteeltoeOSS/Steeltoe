// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Host;

internal static class TestConfiguration
{
    public static string CloudFoundryRabbitMqConfiguration => @"
        {
            ""p-rabbitmq"": [{
                ""credentials"": {
                    ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                },
                ""syslog_drain_url"": null,
                ""label"": ""p-rabbitmq"",
                ""provider"": null,
                ""plan"": ""standard"",
                ""name"": ""myRabbitMQService1"",
                ""tags"": [
                    ""rabbitmq"",
                    ""amqp""
                ]
            }]
        }";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.TestResources;

public static class TestSettingsFactory
{
    public static IDictionary<string, string?> Get(FastTestConfigurations configurations)
    {
        Dictionary<string, string?> settings = [];

        if (configurations.HasFlag(FastTestConfigurations.ConfigServer))
        {
            settings["spring:cloud:config:enabled"] = "false";
        }

        if (configurations.HasFlag(FastTestConfigurations.Discovery))
        {
            settings["consul:discovery:enabled"] = "false";
            settings["consul:discovery:hostname"] = "localhost";
            settings["consul:discovery:ipaddress"] = "127.0.0.1";
            settings["eureka:client:enabled"] = "false";
            settings["eureka:client:serviceUrl"] = "http://127.0.0.1";
            settings["eureka:instance:hostname"] = "localhost";
            settings["eureka:instance:ipaddress"] = "127.0.0.1";
        }

        if (configurations.HasFlag(FastTestConfigurations.Connectors))
        {
            settings["Steeltoe:Client:Mysql:Default:ConnectionString"] = "host=unknown.host.some";
            settings["Steeltoe:Client:RabbitMQ:Default:ConnectionString"] = "amqp://unknown.host.some";
        }

        return settings.ToImmutableDictionary();
    }

    public static IConfigurationBuilder Add(this IConfigurationBuilder builder, FastTestConfigurations testConfigurations)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IDictionary<string, string?> settings = Get(testConfigurations);

        if (settings.Count > 0)
        {
            builder.AddInMemoryCollection(settings);
        }

        return builder;
    }
}

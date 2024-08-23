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
            settings["eureka:client:enabled"] = "false";
            settings["eureka:client:serviceUrl"] = "http://127.0.0.1";
        }

        if (configurations.HasFlag(FastTestConfigurations.WaveFrontExport))
        {
            settings["management:metrics:export:wavefront:uri"] = "proxy://localhost:7828";
            settings["management:endpoints:actuator:exposure:include:0"] = "*";
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

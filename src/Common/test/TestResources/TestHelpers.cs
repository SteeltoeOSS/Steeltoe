// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Steeltoe.Common.TestResources;

public static class TestHelpers
{
    public static readonly ImmutableDictionary<string, string?> FastTestsConfiguration = new Dictionary<string, string?>
    {
        { "spring:cloud:config:enabled", "false" },
        { "eureka:client:serviceUrl", "http://127.0.0.1" },
        { "eureka:client:enabled", "false" },
        { "consul:discovery:enabled", "false" },
        { "management:endpoints:actuator:exposure:include:0", "*" },
        { "management:metrics:export:wavefront:uri", "proxy://localhost:7828" }
    }.ToImmutableDictionary();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.EndpointWeb.Test
{
    internal class DefaultTestSettingsConfig
    {
        public static readonly Settings DefaultSettings = new Settings()
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:info:id"] = "infomanagement",
            ["management:endpoints:actuator:exposure:include:0"] = "*",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0'",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };
    }
}

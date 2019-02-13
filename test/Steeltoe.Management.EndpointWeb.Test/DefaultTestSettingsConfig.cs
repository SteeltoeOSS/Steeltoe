// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            ["info:NET:ASPNET:version"] = "2.0.0"
        };
    }
}

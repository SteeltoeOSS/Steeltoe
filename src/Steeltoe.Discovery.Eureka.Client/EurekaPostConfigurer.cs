//
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
//

using System;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaPostConfigurer
    {
        public const string SPRING_APPLICATION_NAME_KEY = "spring:application:name";
        public const string SPRING_APPLICATION_INSTANCEID_KEY = "spring:application:instance_id";
        public const string SPRING_CLOUD_DISCOVERY_REGISTRATIONMETHOD_KEY = "spring:cloud:discovery:registrationMethod";

        public static void UpdateConfiguration(IConfiguration config, EurekaInstanceOptions options)
        {
            var defaultId = options.GetHostName(false) + ":" + options.AppName + ":" + options.NonSecurePort;

            if (EurekaInstanceOptions.Default_Appname.Equals(options.AppName))
            {
                string springAppName = config.GetValue<string>(SPRING_APPLICATION_NAME_KEY);
                if (!string.IsNullOrEmpty(springAppName))
                {
                    options.AppName = springAppName;
                    options.VirtualHostName = springAppName;
                    options.SecureVirtualHostName = springAppName;
                }
            }
 
            if (defaultId.Equals(options.InstanceId))
            {
                string springInstanceId = config.GetValue<string>(SPRING_APPLICATION_INSTANCEID_KEY);
                if (!string.IsNullOrEmpty(springInstanceId))
                {
                    options.InstanceId = springInstanceId;
                }
            }

            if (string.IsNullOrEmpty(options.RegistrationMethod))
            {
                string springRegMethod = config.GetValue<string>(SPRING_CLOUD_DISCOVERY_REGISTRATIONMETHOD_KEY);
                if (!string.IsNullOrEmpty(springRegMethod))
                {
                    options.RegistrationMethod = springRegMethod;
                }
            }

        }
    }
}

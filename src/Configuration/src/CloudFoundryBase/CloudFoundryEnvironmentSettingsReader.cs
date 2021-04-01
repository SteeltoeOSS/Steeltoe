// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryEnvironmentSettingsReader : ICloudFoundrySettingsReader
    {
        private const string CF_INSTANCE_GUID = "CF_INSTANCE_GUID";

        private const string CF_INSTANCE_INDEX = "CF_INSTANCE_INDEX";

        private const string CF_INSTANCE_INTERNAL_IP = "CF_INSTANCE_INTERNAL_IP";

        private const string CF_INSTANCE_IP = "CF_INSTANCE_IP";

        private const string CF_INSTANCE_PORT = "CF_INSTANCE_PORT";

        private const string VCAP_APPLICATION = "VCAP_APPLICATION";

        private const string VCAP_SERVICES = "VCAP_SERVICES";

        private const string SPRING_APPLICATION_JSON = "SPRING_APPLICATION_JSON";

        public string ApplicationJson => Environment.GetEnvironmentVariable(VCAP_APPLICATION);

        public string InstanceId => Environment.GetEnvironmentVariable(CF_INSTANCE_GUID);

        public string InstanceIndex => Environment.GetEnvironmentVariable(CF_INSTANCE_INDEX);

        public string InstanceInternalIp => Environment.GetEnvironmentVariable(CF_INSTANCE_INTERNAL_IP);

        public string InstanceIp => Environment.GetEnvironmentVariable(CF_INSTANCE_IP);

        public string InstancePort => Environment.GetEnvironmentVariable(CF_INSTANCE_PORT);

        public string ServicesJson => Environment.GetEnvironmentVariable(VCAP_SERVICES);

        public string SpringApplicationJson => Environment.GetEnvironmentVariable(SPRING_APPLICATION_JSON);
    }
}

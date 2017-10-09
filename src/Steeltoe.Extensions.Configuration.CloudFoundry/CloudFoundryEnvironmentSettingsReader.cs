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

        public string ApplicationJson => Environment.GetEnvironmentVariable(VCAP_APPLICATION);

        public string InstanceId => Environment.GetEnvironmentVariable(CF_INSTANCE_GUID);

        public string InstanceIndex => Environment.GetEnvironmentVariable(CF_INSTANCE_INDEX);

        public string InstanceInternalIp => Environment.GetEnvironmentVariable(CF_INSTANCE_INTERNAL_IP);

        public string InstanceIp => Environment.GetEnvironmentVariable(CF_INSTANCE_IP);

        public string InstancePort => Environment.GetEnvironmentVariable(CF_INSTANCE_PORT);

        public string ServicesJson => Environment.GetEnvironmentVariable(VCAP_SERVICES);
    }
}

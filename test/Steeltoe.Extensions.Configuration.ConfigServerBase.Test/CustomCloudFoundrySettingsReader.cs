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

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    internal class CustomCloudFoundrySettingsReader : ICloudFoundrySettingsReader
    {
        public string ApplicationJson => throw new NotImplementedException();

        public string InstanceId => throw new NotImplementedException();

        public string InstanceIndex => throw new NotImplementedException();

        public string InstanceInternalIp => throw new NotImplementedException();

        public string InstanceIp => throw new NotImplementedException();

        public string InstancePort => throw new NotImplementedException();

        public string ServicesJson => throw new NotImplementedException();
    }
}

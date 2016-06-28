//
// Copyright 2015 the original author or authors.
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
using SteelToe.CloudFoundry.Connector.App;

namespace SteelToe.CloudFoundry.Connector.Services
{
    public abstract class ServiceInfo : IServiceInfo
    {
        public ServiceInfo(string id) :
            this(id, null)
        {
   
        }
        public ServiceInfo(string id, ApplicationInstanceInfo info)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            ApplicationInfo = info;
        }

        public string Id { get; internal protected set; }

        public IApplicationInstanceInfo ApplicationInfo { get; internal protected set; }
    }
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint.Env
{
    [Obsolete]
    public class EnvOptions : AbstractOptions, IEnvOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:env";

        public EnvOptions()
            : base()
        {
            Id = "env";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public EnvOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "env";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }
        }

        public string[] KeysToSanitize => throw new NotImplementedException();
    }
}

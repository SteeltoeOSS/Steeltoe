﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Test
{
    internal class TestOptions : IEndpointOptions
    {
        public string Id { get; set; }

        public bool? Enabled { get; set; }

        public IManagementOptions Global { get; set; }

        public string Path { get; set; }

        public Permissions RequiredPermissions { get; set; }

        public bool IsEnabled => Enabled.Value;

        public bool IsSensitive => throw new System.NotImplementedException();

        public bool? Sensitive => throw new System.NotImplementedException();

        public bool IsAccessAllowed(Permissions permissions)
        {
            return false;
        }
    }
}

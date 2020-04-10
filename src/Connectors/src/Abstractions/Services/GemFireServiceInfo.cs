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

using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    public class GemFireServiceInfo : ServiceInfo
    {
        public GemFireServiceInfo(string id)
            : base(id, null)
        {
        }

        public List<string> Locators { get; set; } = new List<string>();

        public Dictionary<string, string> Urls { get; set; } = new Dictionary<string, string>();

        public List<GemFireUser> Users { get; set; } = new List<GemFireUser>();
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class GemFireUser
    {
        public string Password { get; set; }

        public IEnumerable<string> Roles { get; set; }

        public string Username { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single class
}

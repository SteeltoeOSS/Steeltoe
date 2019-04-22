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

using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class Service
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public string[] Tags { get; set; }

        public string Plan { get; set; }

        public Dictionary<string, Credential> Credentials { get; set; }
    }
}

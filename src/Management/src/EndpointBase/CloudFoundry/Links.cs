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

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    // TODO: Move this to the Hypermedia namespace in 3.0
    [Obsolete("This will move to Hypermedia namespace")]
    public class Links
    {
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
        public string type = "steeltoe";
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
        public Dictionary<string, Link> _links = new Dictionary<string, Link>();
    }
}

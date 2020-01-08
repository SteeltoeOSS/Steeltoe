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

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    /// <summary>
    /// A typed collection of links
    /// </summary>
    public class Links
    {
        /// <summary>
        /// Gets or sets the type of links contained in this collection
        /// </summary>
        public string Type { get; set; } = "steeltoe";

        /// <summary>
        /// Gets or sets the list of links contained in this collection
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public Dictionary<string, Link> _links { get; set; } = new Dictionary<string, Link>();
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}

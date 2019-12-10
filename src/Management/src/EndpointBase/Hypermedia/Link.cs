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

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class Link
    {
#pragma warning disable SA1300 // Accessible fields must begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public string href { get; private set; }

        public bool templated { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Accessible fields must begin with upper-case letter

        public Link(string href)
        {
            this.href = href;
            templated = href.Contains("{");
        }
    }
}

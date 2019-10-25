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

using Microsoft.Extensions.Hosting;
#if NETSTANDARD2_1
using Microsoft.Extensions.Hosting.Internal;
#else
using Microsoft.AspNetCore.Hosting.Internal;
#endif

namespace Steeltoe.Common
{
    public static class HostingHelpers
    {
#if NETSTANDARD2_1
        public static IHostEnvironment GetHostingEnvironment(string environmentName = "EnvironmentName")
#else
        public static IHostingEnvironment GetHostingEnvironment(string environmentName = "EnvironmentName")
#endif
        {
            return new HostingEnvironment() { EnvironmentName = environmentName };
        }
    }
}

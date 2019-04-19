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

using System;
using System.Runtime.InteropServices;

namespace Steeltoe.Common
{
    public static class Platform
    {
        public const string NET_FRAMEWORK = ".NET Framework";
        public const string NET_CORE = ".NET Core";
        public const string VCAP_APPLICATION = "VCAP_APPLICATION";

        public static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(NET_FRAMEWORK);

        public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(NET_CORE);

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsCloudFoundry => Environment.GetEnvironmentVariable(VCAP_APPLICATION) != null;
    }
}

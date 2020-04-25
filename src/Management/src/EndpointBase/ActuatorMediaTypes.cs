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
using System.Linq;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorMediaTypes
    {
        public static readonly string V1_JSON = "application/vnd.spring-boot.actuator.v1+json";

        public static readonly string V2_JSON = "application/vnd.spring-boot.actuator.v2+json";

        public static readonly string APP_JSON = "application/json";

        public static readonly string ANY = "*/*";

        public static string GetContentHeaders(List<string> acceptHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            string contentHeader = APP_JSON;
            string versionContentHeader = GetContentTypeHeaderForVersion(version);

            if (acceptHeaders != null && acceptHeaders.Any(x => x == ANY || x == versionContentHeader))
            {
                contentHeader = versionContentHeader;
            }

            return contentHeader += ";charset=UTF-8";
        }

        private static string GetContentTypeHeaderForVersion(MediaTypeVersion version = MediaTypeVersion.V2)
        {
            return version == MediaTypeVersion.V2 ? V2_JSON : V1_JSON;
        }
    }
}
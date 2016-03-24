//
// Copyright 2015 the original author or authors.
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
//

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;


namespace SteelToe.Discovery.Eureka.Transport
{
    internal class JsonApplications
    {
        public string AppsHashCode { get; private set; }
        public long VersionDelta { get; private set; }

        public IList<JsonApplication> Applications { get; private set; }

        [JsonConstructor]
        public JsonApplications(
                    [JsonProperty("apps__hashcode")] string appsHashCode,
                    [JsonProperty("versions__delta")]  long versionDelta,
                    [JsonProperty("application")] List<JsonApplication> applications)
        {
            AppsHashCode = appsHashCode;
            VersionDelta = versionDelta;
            Applications = applications;
        }

        internal static JsonApplications Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonApplications>(stream);
        }
    }

}


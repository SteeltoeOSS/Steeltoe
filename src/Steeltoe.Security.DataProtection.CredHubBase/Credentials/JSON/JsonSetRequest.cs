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

using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class JsonSetRequest : CredentialSetRequest
    {
        public JsonSetRequest(string credentialName, JObject value, List<CredentialPermission> additionalPermissions = null, bool overwrite = false)
        {
            Name = credentialName;
            Type = CredentialType.JSON;
            Value = new JsonCredential(value);
            AdditionalPermissions = additionalPermissions;
            Overwrite = overwrite;
        }

        public JsonSetRequest(string credentialName, string value, List<CredentialPermission> additionalPermissions = null, bool overwrite = false)
        {
            Name = credentialName;
            Type = CredentialType.JSON;
            Value = new JsonCredential(value);
            AdditionalPermissions = additionalPermissions;
            Overwrite = overwrite;
        }
    }
}

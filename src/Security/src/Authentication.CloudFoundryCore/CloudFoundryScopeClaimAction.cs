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

using Microsoft.AspNetCore.Authentication.OAuth.Claims;
#if NETSTANDARD2_0
using Newtonsoft.Json.Linq;
#endif
using System.Security.Claims;
#if NETCOREAPP3_0
using System.Text.Json;
#endif

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryScopeClaimAction : ClaimAction
    {
        public CloudFoundryScopeClaimAction(string claimType, string valueType)
            : base(claimType, valueType)
        {
        }

#if NETCOREAPP3_0
        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
#else
        public override void Run(JObject userData, ClaimsIdentity identity, string issuer)
#endif
        {
            var scopes = CloudFoundryHelper.GetScopes(userData);
            if (scopes != null)
            {
                foreach (var s in scopes)
                {
                    identity.AddClaim(new Claim(ClaimType, s, ValueType, issuer));
                }
            }
        }
    }
}

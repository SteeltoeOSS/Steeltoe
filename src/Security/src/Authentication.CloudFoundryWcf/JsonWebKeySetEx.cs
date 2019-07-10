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

using Microsoft.IdentityModel.Tokens;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    [Obsolete("This class is only used by obsolete code and will be removed in a future release")]
    public class JsonWebKeySetEx : JsonWebKeySet
    {
        public JsonWebKeySetEx(string json)
            : base(json)
        {
            // try to see if its just one key not the set
            if (Keys == null || Keys.Count == 0)
            {
                JsonWebKey key = new JsonWebKey(json);
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                if (key != null)
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
                {
                    Keys.Add(key);
                }
            }
        }
    }
}

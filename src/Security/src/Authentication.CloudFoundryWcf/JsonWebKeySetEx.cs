// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                var key = new JsonWebKey(json);
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

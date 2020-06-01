// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Steeltoe.Security.DataProtection.CredHub
{
    [JsonConverter(typeof(JsonCredentialJsonConverter))]
    public class JsonCredential : ICredentialValue
    {
        public JsonCredential(JObject value)
        {
            Value = value;
        }

        public JsonCredential(string valueAsString)
        {
            Value = JObject.Parse(valueAsString);
        }

        public JObject Value { get; private set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return Value.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
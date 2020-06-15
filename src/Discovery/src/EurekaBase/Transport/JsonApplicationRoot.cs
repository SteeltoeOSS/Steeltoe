// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplicationRoot
    {
        public JsonApplication Application { get; set; }

        internal static JsonApplicationRoot Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonApplicationRoot>(stream);
        }
    }
}

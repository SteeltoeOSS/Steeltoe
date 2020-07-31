// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal static class JsonSerialization
    {
        internal static T Deserialize<T>(Stream stream)
        {
            using JsonReader reader = new JsonTextReader(new StreamReader(stream));
            var serializer = new JsonSerializer();
            return (T)serializer.Deserialize(reader, typeof(T));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Steeltoe.Common.Http
{
    public static class SerializationHelper
    {
        public static T Deserialize<T>(Stream stream, ILogger logger = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                using JsonReader reader = new JsonTextReader(new StreamReader(stream));
                var serializer = new JsonSerializer();
                return (T)serializer.Deserialize(reader, typeof(T));
            }
            catch (Exception e)
            {
                logger?.LogError("Serialization exception: {0}", e);
            }

            return default;
        }
    }
}

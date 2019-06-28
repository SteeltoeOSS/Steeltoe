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

using Newtonsoft.Json;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal static class JsonSerialization
    {
        internal static T Deserialize<T>(Stream stream)
        {
            using (JsonReader reader = new JsonTextReader(new StreamReader(stream)))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (T)serializer.Deserialize(reader, typeof(T));
            }
        }
    }
}

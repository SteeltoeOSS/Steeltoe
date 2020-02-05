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
using Newtonsoft.Json.Serialization;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;
using System.IO;

namespace Steeltoe.Stream.Tck
{
    public class TypelessMessageToPojoStreamListener
    {
        [StreamListener(IProcessor.INPUT)]
        [SendTo(IProcessor.OUTPUT)]
        public Person Echo(IMessage value)
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            // assume it is string because CT is text/plain
            var serializer = JsonSerializer.Create(settings);
            var textReader = new StringReader((string)value.Payload);
            return (Person)serializer.Deserialize(textReader, typeof(Person));
        }
    }
}

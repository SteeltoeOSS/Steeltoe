// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Integration.Attributes;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Messaging;
using System.IO;

namespace Steeltoe.Stream.Tck
{
    public class ByteArrayMessageToStringJsonMessageServiceActivator
    {
        [ServiceActivator(InputChannel = IProcessor.INPUT, OutputChannel = IProcessor.OUTPUT)]
        public IMessage<string> Echo(IMessage<byte[]> value)
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            // assume it is string because CT is text/plain
            var serializer = JsonSerializer.Create(settings);
            var textReader = new StreamReader(new MemoryStream(value.Payload), true);
            var person = (Person)serializer.Deserialize(textReader, typeof(Person));
            person.Name = "bob";
            var writer = new StringWriter();
            serializer.Serialize(writer, person);

            return (IMessage<string>)MessageBuilder.WithPayload<string>(writer.ToString()).Build();
        }
    }
}

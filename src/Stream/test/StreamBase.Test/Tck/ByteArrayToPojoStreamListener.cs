// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;
using System.IO;

namespace Steeltoe.Stream.Tck;

public class ByteArrayToPojoStreamListener
{
    [StreamListener(ISink.InputName)]
    [SendTo(ISource.OutputName)]
    public Person Echo(byte[] value)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var serializer = JsonSerializer.Create(settings);
        var textReader = new StreamReader(new MemoryStream(value), true);
        return (Person)serializer.Deserialize(textReader, typeof(Person));
    }
}

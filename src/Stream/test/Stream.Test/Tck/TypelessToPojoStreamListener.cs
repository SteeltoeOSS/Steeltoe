// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Tck;

public class TypelessToPojoStreamListener
{
    [StreamListener(ISink.InputName)]
    [SendTo(ISource.OutputName)]
    public Person Echo(object value)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // assume it is string because CT is text/plain
        var serializer = JsonSerializer.Create(settings);
        var textReader = new StringReader((string)value);
        return (Person)serializer.Deserialize(textReader, typeof(Person));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Xunit;

namespace Steeltoe.Security.DataProtection.CredHub.Test;

public class CredentialJsonConverterTests
{
    [Fact]
    public void ValueConverter_SerializesClass_AsStringProperty()
    {
        var passwordCredential = new PasswordCredential("thisIsAPassword");
        var chClient = new CredHubClient();

        string serialized = JsonSerializer.Serialize(passwordCredential, chClient.SerializerOptions);

        Assert.Equal("\"thisIsAPassword\"", serialized);
    }

    [Fact]
    public void ValueConverter_Deserializes_StringProperty_AsClass()
    {
        const string serialized = "\"thisIsAValue\"";

        var valueCredential = JsonSerializer.Deserialize<ValueCredential>(serialized);

        Assert.NotNull(valueCredential);
        Assert.Equal("thisIsAValue", valueCredential.ToString());
    }
}

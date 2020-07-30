// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Xunit;

namespace Steeltoe.Security.DataProtection.CredHub.Test
{
    public class CredentialJsonConverterTests
    {
        [Fact]
        public void ValueConverter_SerializesClass_AsStringProperty()
        {
            // arrange
            var passwordCredential = new PasswordCredential("thisIsAPassword");
            var chClient = new CredHubClient();

            // act
            var serialized = JsonSerializer.Serialize(passwordCredential, chClient.SerializerOptions);

            // assert
            Assert.Equal("\"thisIsAPassword\"", serialized);
        }

        [Fact]
        public void ValueConverter_Deserializes_StringProperty_AsClass()
        {
            // arrange
            var serialized = "\"thisIsAValue\"";

            // act
            var valueCredential = JsonSerializer.Deserialize<ValueCredential>(serialized);

            // assert
            Assert.NotNull(valueCredential);
            Assert.Equal("thisIsAValue", valueCredential.ToString());
        }
    }
}

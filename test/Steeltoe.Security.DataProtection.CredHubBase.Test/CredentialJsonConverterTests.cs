// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
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

            // act
            var serialized = JsonConvert.SerializeObject(passwordCredential);

            // assert
            Assert.Equal("\"thisIsAPassword\"", serialized);
        }

        [Fact]
        public void ValueConverter_Deserializes_StringProperty_AsClass()
        {
            // arrange
            var serialized = "\"thisIsAValue\"";

            // act
            var valueCredential = JsonConvert.DeserializeObject<ValueCredential>(serialized);

            // assert
            Assert.NotNull(valueCredential);
            Assert.Equal("thisIsAValue", valueCredential.ToString());
        }
    }
}

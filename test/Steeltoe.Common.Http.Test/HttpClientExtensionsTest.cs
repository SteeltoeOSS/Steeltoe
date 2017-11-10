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

using RichardSzalay.MockHttp;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    public class HttpClientExtensionsTest
    {
        [Fact]
        public async void PostAsJsonAsync_Issues_POST_WithJSON()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var authRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, "http://test-server/*").Respond("application/json", "{\"Name\": \"Test\", \"Value\": \"Poster\"}");
            var client = new HttpClient(mockHttpMessageHandler);

            // act
            var result = await client.PostAsJsonAsync("http://test-server/", new SerializationTestObject { Name = "Sample", Value = "Test" });
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("Poster", responseMessage.Value);
        }

        [Fact]
        public async void PutAsJsonAsync_Issues_PUT_WithJSON()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var authRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, "http://test-server/*").Respond("application/json", "{\"Name\": \"Test\", \"Value\": \"PutPut\"}");
            var client = new HttpClient(mockHttpMessageHandler);

            // act
            var result = await client.PutAsJsonAsync("http://test-server/", new SerializationTestObject { Name = "Sample", Value = "Test" });
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("PutPut", responseMessage.Value);
        }
    }
}

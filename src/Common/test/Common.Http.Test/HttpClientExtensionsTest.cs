// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RichardSzalay.MockHttp;
using System;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    [Obsolete]
    public class HttpClientExtensionsTest
    {
        [Fact]
        public async void PostAsJsonAsync_Issues_POST_WithJSON()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Post, "http://test-server/*")
                .WithContent("{\"Name\":\"Sample\",\"Value\":\"Request\"}")
                .Respond("application/json", "{\"name\": \"Test\", \"value\": \"Response\"}");
            var client = new HttpClient(mockHttpMessageHandler);

            // act
            var result = await client.PostAsJsonAsync("http://test-server/", new SerializationTestObject { Name = "Sample", Value = "Request" });
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("Response", responseMessage.Value);
        }

        [Fact]
        public async void PostAsJsonAsync_Uses_GivenSerializerSettings()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var authRequest = mockHttpMessageHandler
                .Expect(HttpMethod.Post, "http://test-server/*")
                .WithContent("{\"name\":\"Sample\",\"value\":\"Request\"}")
                .Respond("application/json", "{\"name\": \"Test\", \"value\": \"Response\"}");
            var client = new HttpClient(mockHttpMessageHandler);
            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var requestObject = new SerializationTestObject { Name = "Sample", Value = "Request" };

            // act
            var result = await client.PostAsJsonAsync("http://test-server/", requestObject, serializerSettings);
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("Response", responseMessage.Value);
        }

        [Fact]
        public async void PutAsJsonAsync_Issues_PUT_WithJSON()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Put, "http://test-server/*")
                .WithContent("{\"Name\":\"Sample\",\"Value\":\"Request\"}")
                .Respond("application/json", "{\"Name\": \"Test\", \"Value\": \"Response\"}");
            var client = new HttpClient(mockHttpMessageHandler);

            // act
            var result = await client.PutAsJsonAsync("http://test-server/", new SerializationTestObject { Name = "Sample", Value = "Request" });
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("Response", responseMessage.Value);
        }

        [Fact]
        public async void PutAsJsonAsync_Uses_GivenSerializerSettings()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var authRequest = mockHttpMessageHandler
                .Expect(HttpMethod.Put, "http://test-server/*")
                .WithContent("{\"name\":\"Sample\",\"value\":\"Request\"}")
                .Respond("application/json", "{\"name\": \"Test\", \"value\": \"Response\"}");
            var client = new HttpClient(mockHttpMessageHandler);
            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var requestObject = new SerializationTestObject { Name = "Sample", Value = "Request" };

            // act
            var result = await client.PutAsJsonAsync("http://test-server/", requestObject, serializerSettings);
            var responseMessage = await result.Content.ReadAsJsonAsync<SerializationTestObject>();

            // assert
            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("Test", responseMessage.Name);
            Assert.Equal("Response", responseMessage.Value);
        }
    }
}

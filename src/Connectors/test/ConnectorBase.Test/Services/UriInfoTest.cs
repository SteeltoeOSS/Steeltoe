// Copyright 2017 the original author or authors.
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

using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class UriInfoTest
    {
        [Fact]
        public void Constructor_Uri()
        {
            var uri = "mysql://joe:joes_password@localhost:1527/big_db";
            var result = new UriInfo(uri);

            AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", null);
            Assert.Equal(uri, result.UriString);
        }

        [Fact]
        public void Constructor_WithQuery()
        {
            var uri = "mysql://joe:joes_password@localhost:1527/big_db?p1=v1&p2=v2";
            var result = new UriInfo(uri);

            AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", "p1=v1&p2=v2");
            Assert.Equal(uri, result.UriString);
        }

        [Fact]
        public void Constructor_NoUsernamePassword()
        {
            var uri = "mysql://localhost:1527/big_db";
            var result = new UriInfo(uri);

            AssertUriInfoEquals(result, "localhost", 1527, null, null, "big_db", null);
            Assert.Equal(uri, result.UriString);
        }

        [Fact]
        public void Constructor_WithUsernameNoPassword()
        {
            var uri = "mysql://joe@localhost:1527/big_db";
            var ex = Assert.Throws<ArgumentException>(() => new UriInfo(uri));
            Assert.Contains("joe", ex.Message);
        }

        [Fact]
        public void Constructor_WithExplicitParameters()
        {
            var uri = "mysql://joe:joes_password@localhost:1527/big_db";
            var result = new UriInfo("mysql", "localhost", 1527, "joe", "joes_password", "big_db");

            AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", null);
            Assert.Equal(uri, result.UriString);
        }

        private void AssertUriInfoEquals(UriInfo result, string host, int port, string username, string password, string path, string query)
        {
            Assert.Equal(host, result.Host);
            Assert.Equal(port, result.Port);
            Assert.Equal(username, result.UserName);
            Assert.Equal(password, result.Password);
            Assert.Equal(path, result.Path);
            Assert.Equal(query, result.Query);
        }
    }
}

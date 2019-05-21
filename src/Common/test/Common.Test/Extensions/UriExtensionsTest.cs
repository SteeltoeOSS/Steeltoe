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

using Steeltoe.Common.Extensions;
using System;
using Xunit;

namespace Steeltoe.Common.Test.Extensions
{
    public class UriExtensionsTest
    {
        [Fact]
        public void MaskExistingBasicAuthenticationToString()
        {
            // Arrange
            var uri = new Uri("http://username:password@www.example.com/");
            var expected = "http://****:****@www.example.com/";

            // Act
            var masked = uri.ToMaskedString();

            // Assert
            Assert.Equal(expected, masked);
        }

        [Fact]
        public void MaskExistingBasicAuthentication()
        {
            // Arrange
            var uri = new Uri("http://username:password@www.example.com/");
            var expected = new Uri("http://****:****@www.example.com/");

            // Act
            var masked = uri.ToMaskedUri();

            // Assert
            Assert.Equal(expected, masked);
        }

        [Fact]
        public void DontMaskStringIfNotBasicAuthenticationExists()
        {
            // Arrange
            var uri = new Uri("http://www.example.com/");
            var expected = uri.ToString();

            // Act
            var masked = uri.ToMaskedString();

            // Assert
            Assert.Equal(expected, masked);
        }

        [Fact]
        public void DontMaskUriIfNotBasicAuthenticationExists()
        {
            // Arrange
            var uri = new Uri("http://www.example.com/");
            var expected = new Uri(uri.ToString());

            // Act
            var masked = uri.ToMaskedUri();

            // Assert
            Assert.Equal(expected, masked);
        }
    }
}

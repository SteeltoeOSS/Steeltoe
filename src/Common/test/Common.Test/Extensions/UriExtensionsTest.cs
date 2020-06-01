// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

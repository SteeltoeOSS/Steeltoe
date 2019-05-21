using Steeltoe.Common.Extensions;
using System;
using Xunit;

namespace Steeltoe.Common.Test.Extensions
{
    public class UriExtensionsTest
    {
        [Fact]
        public void MaskExistingBasicAuthentication()
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
        public void DontMaskIfNotBasicAuthenticationExists()
        {
            // Arrange
            var uri = new Uri("http://www.example.com/");
            var expected = uri.ToString();

            // Act
            var masked = uri.ToMaskedString();

            // Assert
            Assert.Equal(expected, masked);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.Common.Discovery.Test
{
    public class DiscoveryOptionsTest
    {
        [Fact]
        public void Constructor_Initializes_ClientType_Unknown()
        {
            var option = new DiscoveryOptions();
            Assert.Equal(DiscoveryClientType.UNKNOWN, option.ClientType);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }
    }
}

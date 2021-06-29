﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class ServiceInfoTest
    {
        [Fact]
        public void Constructor_ThrowsIfIdNull()
        {
            string id = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfo(id));
            Assert.Contains(nameof(id), ex.Message);
        }

        [Fact]
        public void Constructor_InitializesValues()
        {
            var info = new ApplicationInstanceInfo(TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>()));
            var si = new TestServiceInfo("id", info);
            Assert.Equal("id", si.Id);
            Assert.Equal(info, si.ApplicationInfo);
        }
    }
}

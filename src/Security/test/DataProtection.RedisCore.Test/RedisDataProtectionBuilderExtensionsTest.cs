// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection;
using System;
using Xunit;

namespace Steeltoe.Security.DataProtection.Redis.Test
{
    public class RedisDataProtectionBuilderExtensionsTest
    {
        [Fact]
        public void PersistKeysToRedis_ThowsForNulls()
        {
            IDataProtectionBuilder builder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => builder.PersistKeysToRedis());
            Assert.Contains(nameof(builder), ex.Message);
        }
    }
}

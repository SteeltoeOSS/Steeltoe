// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using System;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test
{
    public class CloudFoundryApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudFoundryActuators_ThrowsIfNulls()
        {
            IApplicationBuilder builder = null;

            Assert.Throws<ArgumentNullException>(() => builder.UseCloudFoundryActuators());
        }
    }
}

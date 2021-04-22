// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class DataCenterInfoTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_InitsName()
        {
            var dinfo = new DataCenterInfo(DataCenterName.MyOwn);
            Assert.Equal(DataCenterName.MyOwn, dinfo.Name);
        }
    }
}

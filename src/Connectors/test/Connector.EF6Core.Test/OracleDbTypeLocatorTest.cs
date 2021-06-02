// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Oracle;
using Xunit;

#if NET461
namespace Steeltoe.Connector.EF6Core.OracleDb.Test
{
    public class OracleDbTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionType()
        {
            // arrange -- handled by including a compatible NuGet package

            // act
            var type = OracleTypeLocator.OracleConnection;

            // assert
            Assert.NotNull(type);
        }
    }
}
#endif
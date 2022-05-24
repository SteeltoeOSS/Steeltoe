// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class StandardTypeLocatorTests
    {
        [Fact]
        public void TestImports()
        {
            var locator = new StandardTypeLocator();
            Assert.Equal(typeof(int), locator.FindType("System.Int32"));
            Assert.Equal(typeof(string), locator.FindType("System.String"));

            var prefixes = locator.ImportPrefixes;
            Assert.Single(prefixes);
            Assert.Contains("System", prefixes);
            Assert.DoesNotContain("System.Collections", prefixes);

            Assert.Equal(typeof(bool), locator.FindType("Boolean"));

            // currently does not know about java.util by default
            // assertEquals(java.util.List.class,locator.findType("List"));
            var ex = Assert.Throws<SpelEvaluationException>(() => locator.FindType("StringBuilder"));
            Assert.Equal(SpelMessage.TYPE_NOT_FOUND, ex.MessageCode);
            locator.RegisterImport("System.Text");
            Assert.Equal(typeof(StringBuilder), locator.FindType("StringBuilder"));
        }
    }
}

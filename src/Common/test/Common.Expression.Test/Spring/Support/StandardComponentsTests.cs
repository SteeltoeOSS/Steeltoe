// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring.Support
{
    public class StandardComponentsTests
    {
        [Fact]
        public void TestStandardEvaluationContext()
        {
            StandardEvaluationContext context = new StandardEvaluationContext();
            Assert.NotNull(context.TypeComparator);

            var tc = new StandardTypeComparator();
            context.TypeComparator = tc;
            Assert.Equal(tc, context.TypeComparator);

            var tl = new StandardTypeLocator();
            context.TypeLocator = tl;
            Assert.Equal(tl, context.TypeLocator);
        }

        [Fact]
        public void TestStandardOperatorOverloader()
        {
            var oo = new StandardOperatorOverloader();
            Assert.False(oo.OverridesOperation(Operation.ADD, null, null));
            Assert.Throws<EvaluationException>(() => oo.Operate(Operation.ADD, 2, 3));
        }

        [Fact]
        public void TestStandardTypeLocator()
        {
            StandardTypeLocator tl = new StandardTypeLocator();
            var prefixes = tl.ImportPrefixes;
            Assert.Single(prefixes);
            tl.RegisterImport("System.Collections");
            prefixes = tl.ImportPrefixes;
            Assert.Equal(2, prefixes.Count);
            tl.RemoveImport("System.Collections");
            prefixes = tl.ImportPrefixes;
            Assert.Single(prefixes);
        }

        [Fact]
        public void TestStandardTypeConverter()
        {
            var tc = new StandardTypeConverter();
            tc.ConvertValue(3, typeof(int), typeof(double));
        }
    }
}

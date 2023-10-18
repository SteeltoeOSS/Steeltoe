// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring.Ast;

public sealed class FormatHelperTests
{
    [Fact]
    public void FormatMethodWithSingleArgumentForMessage()
    {
        string message = FormatHelper.FormatMethodForMessage("foo", new List<Type>
        {
            typeof(string)
        });

        Assert.Equal("foo(System.String)", message);
    }

    [Fact]
    public void FormatMethodWithMultipleArgumentsForMessage()
    {
        string message = FormatHelper.FormatMethodForMessage("foo", new List<Type>
        {
            typeof(string),
            typeof(int)
        });

        Assert.Equal("foo(System.String,System.Int32)", message);
    }
}

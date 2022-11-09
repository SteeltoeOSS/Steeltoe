// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Test.Support;

public class ErrorMessageTest
{
    [Fact]
    public void TestToString()
    {
        var em = new ErrorMessage(new Exception("foo"));
        string emString = em.ToString();
        Assert.DoesNotContain("original", emString, StringComparison.Ordinal);

        em = new ErrorMessage(new Exception("foo"), Message.Create("bar"));
        emString = em.ToString();
        Assert.Contains("original", emString, StringComparison.Ordinal);
        Assert.Contains(em.OriginalMessage.ToString(), emString, StringComparison.Ordinal);
    }

    [Fact]
    public void TestAnyExceptionType()
    {
        var em = new ErrorMessage(new InvalidOperationException("foo"));
        string emString = em.ToString();
        Assert.Contains("InvalidOperationException", emString, StringComparison.Ordinal);
    }
}

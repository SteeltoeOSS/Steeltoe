// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Messaging.Support.Test;

public class ErrorMessageTest
{
    [Fact]
    public void TestToString()
    {
        var em = new ErrorMessage(new Exception("foo"));
        string emString = em.ToString();
        Assert.DoesNotContain("original", emString);

        em = new ErrorMessage(new Exception("foo"), Message.Create("bar"));
        emString = em.ToString();
        Assert.Contains("original", emString);
        Assert.Contains(em.OriginalMessage.ToString(), emString);
    }

    [Fact]
    public void TestAnyExceptionType()
    {
        var em = new ErrorMessage(new InvalidOperationException("foo"));
        string emString = em.ToString();
        Assert.Contains("InvalidOperationException", emString);
    }
}

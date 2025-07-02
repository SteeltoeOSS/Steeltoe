// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Net;

namespace Steeltoe.Common.Net.Test;

public sealed class WindowsNetworkFileShareTest
{
    [Fact]
    public void GetErrorForKnownNumber_ReturnsKnownError()
    {
        Action action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(5, "execute");

        action.Should().ThrowExactly<IOException>().WithInnerExceptionExactly<Win32Exception>()
            .WithMessage(Platform.IsWindows ? "Access is Denied*" : "Input/output error");

        action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(1222, "execute");

        string message = "The network is not present or not started.";

        if (Platform.IsOSX)
        {
            message = "Unknown error: 1222";
        }
        else if (Platform.IsLinux)
        {
            message = "Unknown error 1222";
        }

        action.Should().ThrowExactly<IOException>().WithInnerExceptionExactly<Win32Exception>().WithMessage(message);
    }

    [Fact]
    public void GetErrorForUnknownNumber_ReturnsUnKnownError()
    {
        Action action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(9999, "execute");

        string message = "Unknown error (0x270f)";

        if (Platform.IsOSX)
        {
            message = "Unknown error: 9999";
        }
        else if (Platform.IsLinux)
        {
            message = "Unknown error 9999";
        }

        action.Should().ThrowExactly<IOException>().WithInnerExceptionExactly<Win32Exception>().WithMessage(message);
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_SetsValuesOn_ConnectSuccess()
    {
        var router = new FakeMultipleProviderRouter();

        _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password"), router);

        router.Username.Should().Be("user");
        router.Password.Should().Be("password");
        router.NetworkPath.Should().Be(@"\\server\path");
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_ConcatenatesUserAndDomain()
    {
        var router = new FakeMultipleProviderRouter();

        _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password", "domain"), router);

        router.Username.Should().Be(@"domain\user");
        router.Password.Should().Be("password");
        router.NetworkPath.Should().Be(@"\\server\path");
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_ThrowsOn_ConnectFail()
    {
        var router = new FakeMultipleProviderRouter(false);

        Action action = () => _ = new WindowsNetworkFileShare("doesn't-matter", new NetworkCredential("user", "password"), router);

        IOException exception = action.Should().ThrowExactly<IOException>().Which;
        exception.InnerException.Should().NotBeNull();

        string message = "The specified device name is invalid.";

        if (Platform.IsOSX)
        {
            message = "Unknown error: 1200";
        }
        else if (Platform.IsLinux)
        {
            message = "Unknown error 1200";
        }

        exception.InnerException.Message.Should().Be(message);
    }
}

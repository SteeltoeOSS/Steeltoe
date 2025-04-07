// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Net;

namespace Steeltoe.Common.Net;

/// <summary>
/// For interacting with SMB network file shares on Windows.
/// </summary>
public sealed class WindowsNetworkFileShare : IDisposable
{
    private const int NoError = 0;
    private readonly string _networkName;
    private readonly IMultipleProviderRouter _multipleProviderRouter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsNetworkFileShare" /> class.
    /// </summary>
    /// <param name="networkName">
    /// The IP address or hostname of the remote file share.
    /// </param>
    /// <param name="credentials">
    /// The username and password for accessing the file share.
    /// </param>
    public WindowsNetworkFileShare(string networkName, NetworkCredential credentials)
        : this(networkName, credentials, new MultipleProviderRouter())
    {
    }

    internal WindowsNetworkFileShare(string networkName, NetworkCredential credentials, IMultipleProviderRouter multipleProviderRouter)
    {
        ArgumentNullException.ThrowIfNull(networkName);
        ArgumentNullException.ThrowIfNull(credentials);

        _networkName = networkName;
        _multipleProviderRouter = multipleProviderRouter;

        var netResource = new NativeMethods.NetResource
        {
            Scope = NativeMethods.ResourceScope.GlobalNetwork,
            ResourceType = NativeMethods.ResourceType.Disk,
            DisplayType = NativeMethods.ResourceDisplayType.Share,
            RemoteName = networkName
        };

        string userName = string.IsNullOrEmpty(credentials.Domain) ? credentials.UserName : $@"{credentials.Domain}\{credentials.UserName}";

        int result = _multipleProviderRouter.UseConnection(IntPtr.Zero, netResource, credentials.Password, userName, 0, null, null, null);
        ThrowForNonZeroResult(result, "connect to network share");
    }

    /// <summary>
    /// Disconnects the file share.
    /// </summary>
    public void Dispose()
    {
        // With the current design, it's not possible to disconnect the network share from the finalizer,
        // because the _multipleProviderRouter instance may have already been garbage-collected.

        int result = _multipleProviderRouter.CancelConnection(_networkName, 0, true);
        ThrowForNonZeroResult(result, "disconnect from network share");
    }

    internal static void ThrowForNonZeroResult(int errorNumber, string operation)
    {
        if (errorNumber != NoError)
        {
            var innerException = new Win32Exception(errorNumber);

            throw new IOException($"Failed to {operation}.", innerException);
        }
    }
}

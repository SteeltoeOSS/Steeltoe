// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Runtime.InteropServices;

#pragma warning disable S3874 // "out" and "ref" parameters should not be used

namespace Steeltoe.Common.Net;

/// <summary>
/// For interacting with SMB network file shares on Windows.
/// </summary>
public sealed class WindowsNetworkFileShare : IDisposable
{
    private const int NoError = 0;
    private const int ErrorAccessDenied = 5;
    private const int ErrorAlreadyAssigned = 85;
    private const int ErrorPathNotFound = 53;
    private const int ErrorBadDevice = 1200;
    private const int ErrorBadNetName = 67;
    private const int ErrorBadProvider = 1204;
    private const int ErrorCancelled = 1223;
    private const int ErrorExtendedError = 1208;
    private const int ErrorInvalidAddress = 487;
    private const int ErrorInvalidParameter = 87;
    private const int ErrorInvalidPassword = 86;
    private const int ErrorInvalidPasswordName = 1216;
    private const int ErrorMoreData = 234;
    private const int ErrorNoMoreItems = 259;
    private const int ErrorNoNetOrBadPath = 1203;
    private const int ErrorNoNetwork = 1222;
    private const int ErrorBadProfile = 1206;
    private const int ErrorCannotOpenProfile = 1205;
    private const int ErrorDeviceInUse = 2404;
    private const int ErrorNotConnected = 2250;
    private const int ErrorOpenFiles = 2401;
    private const int ErrorLogonFailure = 1326;

    // Created with excel formula:
    // ="new ErrorClass("&A1&", """&PROPER(SUBSTITUTE(MID(A1,7,LEN(A1)-6), "_", " "))&"""), "
    private static readonly Dictionary<int, string> ErrorMessageLookupTable = new()
    {
        [ErrorAccessDenied] = "Error: Access Denied",
        [ErrorAlreadyAssigned] = "Error: Already Assigned",
        [ErrorBadDevice] = "Error: Bad Device",
        [ErrorBadNetName] = "Error: Bad Net Name",
        [ErrorBadProvider] = "Error: Bad Provider",
        [ErrorCancelled] = "Error: Cancelled",
        [ErrorExtendedError] = "Error: Extended Error",
        [ErrorInvalidAddress] = "Error: Invalid Address",
        [ErrorInvalidParameter] = "Error: Invalid Parameter",
        [ErrorInvalidPassword] = "Error: Invalid Password",
        [ErrorInvalidPasswordName] = "Error: Invalid Password Format",
        [ErrorMoreData] = "Error: More Data",
        [ErrorNoMoreItems] = "Error: No More Items",
        [ErrorNoNetOrBadPath] = "Error: No Net Or Bad Path",
        [ErrorNoNetwork] = "Error: No Network",
        [ErrorBadProfile] = "Error: Bad Profile",
        [ErrorCannotOpenProfile] = "Error: Cannot Open Profile",
        [ErrorDeviceInUse] = "Error: Device In Use",
        [ErrorNotConnected] = "Error: Not Connected",
        [ErrorOpenFiles] = "Error: Open Files",
        [ErrorLogonFailure] = "The user name or password is incorrect",
        [ErrorPathNotFound] = "The network path not found"
    };

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
            if (ErrorMessageLookupTable.TryGetValue(errorNumber, out string? errorMessage))
            {
                throw new ExternalException($"Failed to {operation} with error {errorNumber}: {errorMessage}.");
            }

            throw new ExternalException($"Failed to {operation} with error {errorNumber}.");
        }
    }
}

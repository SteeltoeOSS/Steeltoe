// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Steeltoe.Common.Net;

/// <summary>
/// For interacting with SMB network file shares on Windows.
/// </summary>
public class WindowsNetworkFileShare : IDisposable
{
    // private const int NO_ERROR = 0
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
    private const int ErrorInvalidPasswordname = 1216;
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
    private static readonly ErrorClass[] ErrorList =
    {
        new (ErrorAccessDenied, "Error: Access Denied"),
        new (ErrorAlreadyAssigned, "Error: Already Assigned"),
        new (ErrorBadDevice, "Error: Bad Device"),
        new (ErrorBadNetName, "Error: Bad Net Name"),
        new (ErrorBadProvider, "Error: Bad Provider"),
        new (ErrorCancelled, "Error: Cancelled"),
        new (ErrorExtendedError, "Error: Extended Error"),
        new (ErrorInvalidAddress, "Error: Invalid Address"),
        new (ErrorInvalidParameter, "Error: Invalid Parameter"),
        new (ErrorInvalidPassword, "Error: Invalid Password"),
        new (ErrorInvalidPasswordname, "Error: Invalid Password Format"),
        new (ErrorMoreData, "Error: More Data"),
        new (ErrorNoMoreItems, "Error: No More Items"),
        new (ErrorNoNetOrBadPath, "Error: No Net Or Bad Path"),
        new (ErrorNoNetwork, "Error: No Network"),
        new (ErrorBadProfile, "Error: Bad Profile"),
        new (ErrorCannotOpenProfile, "Error: Cannot Open Profile"),
        new (ErrorDeviceInUse, "Error: Device In Use"),
        new (ErrorExtendedError, "Error: Extended Error"),
        new (ErrorNotConnected, "Error: Not Connected"),
        new (ErrorOpenFiles, "Error: Open Files"),
        new (ErrorLogonFailure, "The user name or password is incorrect"),
        new (ErrorPathNotFound, "The network path not found")
    };

    private readonly string _networkName;
    private readonly IMultipleProviderRouter _multipleProviderRouter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsNetworkFileShare"/> class.
    /// </summary>
    /// <param name="networkName">Address of the file share.</param>
    /// <param name="credentials">Username and password for accessing the file share.</param>
    /// <param name="multipleProviderRouter">A class that handles calls to mpr.dll or performs same operations.</param>
    public WindowsNetworkFileShare(string networkName, NetworkCredential credentials, IMultipleProviderRouter multipleProviderRouter = null)
    {
        _multipleProviderRouter = multipleProviderRouter ?? new MultipleProviderRouter();

        _networkName = networkName;

        var netResource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = networkName
        };

        var userName = string.IsNullOrEmpty(credentials.Domain)
            ? credentials.UserName
            : $@"{credentials.Domain}\{credentials.UserName}";

        var result = _multipleProviderRouter.UseConnection(IntPtr.Zero, netResource, credentials.Password, userName, 0, null, null, null);

        if (result != 0)
        {
            throw new ExternalException($"Error connecting to remote share - Code: {result}, {GetErrorForNumber(result)}");
        }
    }

    /// <summary>
    /// Scope of the file share.
    /// </summary>
    public enum ResourceScope
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    }

    /// <summary>
    /// Type of network resource.
    /// </summary>
    public enum ResourceType
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8,
    }

    /// <summary>
    /// The display options for the network object in a network browsing user interface.
    /// </summary>
    public enum ResourceDisplaytype
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        Shareadmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        Ndscontainer = 0x0b
    }

    /// <summary>
    /// Retrieves the most recent extended error code set by a WNet function.
    /// <para/>Wraps an underlying P/Invoke call to mpr.dll. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora"/>
    /// </summary>
    /// <param name="error">The error code reported by the network provider.</param>
    /// <param name="errorBuf">String variable to receive the description of the error.</param>
    /// <param name="errorBufSize">Size of error buffer.</param>
    /// <param name="nameBuf">String variable to receive the network provider raising the error.</param>
    /// <param name="nameBufSize">Size of name buffer.</param>
    /// <returns>If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.<para/>If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.</returns>
    public int GetLastError(
        out int error,
        out StringBuilder errorBuf,
        int errorBufSize,
        out StringBuilder nameBuf,
        int nameBufSize)
    {
        return _multipleProviderRouter.GetLastError(out error, out errorBuf, errorBufSize, out nameBuf, nameBufSize);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get a description for an error returned by a P/Invoke call.
    /// </summary>
    /// <param name="errNum">Error code.</param>
    /// <returns>An error message.</returns>
    internal static string GetErrorForNumber(int errNum)
    {
        if (!ErrorList.Any(e => e._num == errNum))
        {
            return $"Error: Unknown, {errNum}";
        }
        else
        {
            return ErrorList.First(e => e._num == errNum)._message;
        }
    }

    /// <summary>
    /// Disposes the object, cancels connection with file share.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // With the current design, it's not possible to disconnect the network share from the finalizer,
        // because the _mpr instance may have already been garbage-collected.
        if (disposing)
        {
            _multipleProviderRouter.CancelConnection(_networkName, 0, true);
        }
    }

    private struct ErrorClass
    {
        public int _num;
        public string _message;

        public ErrorClass(int num, string message)
        {
            _num = num;
            _message = message;
        }
    }

    /// <summary>
    /// The NETRESOURCE structure contains information about a network resource.
    /// More info on NetResource: <seealso href="https://msdn.microsoft.com/en-us/c53d078e-188a-4371-bdb9-fc023bc0c1ba"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplaytype DisplayType;
        public int Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using static Steeltoe.Common.Net.WindowsNetworkFileShare;

namespace Steeltoe.Common.Net;

/// <summary>
/// An interface to methods of mpr.dll used by WindowsNetworkFileShare.
/// </summary>
// TODO: [BREAKING] Rename type to IMultipleProviderRouter and remove suppression
#pragma warning disable S101 // Types should be named in PascalCase
public interface IMPR
#pragma warning restore S101 // Types should be named in PascalCase
{
    /// <summary>
    /// Makes a connection to a network resource and can redirect a local device to the network resource.
    /// <para/>P/Invoke call to mpr.dll. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a"/>
    /// </summary>
    /// <param name="netResource">Network resource to interact with.</param>
    /// <param name="password">Password for making the network connection.</param>
    /// <param name="username">Username for making the network connection.</param>
    /// <param name="flags">A set of connection options. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a#parameters"/></param>
    /// <returns>An integer representing the result. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a#return-value"/></returns>
    int AddConnection(NetResource netResource, string password, string username, int flags);

#pragma warning disable S107 // Methods should not have too many parameters
    /// <summary>
    /// Makes a connection to a network resource. Can redirect a local device to a network resource.
    /// <para/>P/Invoke call to mpr.dll. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetuseconnectiona"/>
    /// </summary>
    /// <param name="hwndOwner">Handle to a window that the provider of network resources can use as an owner window for dialog boxes.</param>
    /// <param name="netResource">Network resource to interact with.</param>
    /// <param name="password">A null-terminated string that specifies a password to be used in making the network connection.</param>
    /// <param name="username">A null-terminated string that specifies a user name for making the connection.</param>
    /// <param name="flags">Set of bit flags describing the connection.</param>
    /// <param name="lpAccessName">Pointer to a buffer that receives system requests on the connection.</param>
    /// <param name="lpBufferSize">Pointer to a variable that specifies the size of the lpAccessName buffer, in characters.<para />If the call fails because the buffer is not large enough, the function returns the required buffer size in this location.</param>
    /// <param name="lpResult">Pointer to a variable that receives additional information about the connection.</param>
    /// <returns>An integer representing the result. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetuseconnectiona#return-value"/></returns>
    int UseConnection(IntPtr hwndOwner, NetResource netResource, string password, string username, int flags, string lpAccessName, string lpBufferSize, string lpResult);
#pragma warning restore S107 // Methods should not have too many parameters

    /// <summary>
    /// Cancels an existing network connection, removes remembered network connections that are not currently connected.
    /// <para/>P/Invoke call to mpr.dll. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a"/>
    /// </summary>
    /// <param name="name">
    /// Pointer to a constant null-terminated string that specifies the name of either the redirected local device or the remote network resource to disconnect from.<para/>
    /// If this parameter specifies a redirected local device, the function cancels only the specified device redirection. If the parameter specifies a remote network resource, all connections without devices are canceled.
    /// </param>
    /// <param name="flags">Connection type. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a#parameters"/></param>
    /// <param name="force">Specifies whether the disconnection should occur if there are open files or jobs on the connection. If this parameter is FALSE, the function fails if there are open files or jobs.</param>
    /// <returns>An integer representing the result. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a#return-value"/></returns>
    int CancelConnection(string name, int flags, bool force);

    /// <summary>
    /// Retrieves the most recent extended error code set by a WNet function.
    /// <para/>P/Invoke call to mpr.dll. <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora"/>
    /// </summary>
    /// <param name="error">The error code reported by the network provider.</param>
    /// <param name="errorBuf">String variable to receive the description of the error.</param>
    /// <param name="errorBufSize">Size of error buffer.</param>
    /// <param name="nameBuf">String variable to receive the network provider raising the error.</param>
    /// <param name="nameBufSize">Size of name buffer.</param>
    /// <returns>If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.<para/>If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.</returns>
    int GetLastError(out int error, out StringBuilder errorBuf, int errorBufSize, out StringBuilder nameBuf, int nameBufSize);
}

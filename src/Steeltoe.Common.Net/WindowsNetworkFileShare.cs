// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Steeltoe.Common.Net
{
    /// <summary>
    /// For interacting with SMB network file shares on Windows
    /// </summary>
    public class WindowsNetworkFileShare : IDisposable
    {
        // private const int NO_ERROR = 0;
        private const int ERROR_ACCESS_DENIED = 5;
        private const int ERROR_ALREADY_ASSIGNED = 85;
        private const int ERROR_PATH_NOT_FOUND = 53;
        private const int ERROR_BAD_DEVICE = 1200;
        private const int ERROR_BAD_NET_NAME = 67;
        private const int ERROR_BAD_PROVIDER = 1204;
        private const int ERROR_CANCELLED = 1223;
        private const int ERROR_EXTENDED_ERROR = 1208;
        private const int ERROR_INVALID_ADDRESS = 487;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INVALID_PASSWORD = 86;
        private const int ERROR_INVALID_PASSWORDNAME = 1216;
        private const int ERROR_MORE_DATA = 234;
        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_NO_NET_OR_BAD_PATH = 1203;
        private const int ERROR_NO_NETWORK = 1222;
        private const int ERROR_BAD_PROFILE = 1206;
        private const int ERROR_CANNOT_OPEN_PROFILE = 1205;
        private const int ERROR_DEVICE_IN_USE = 2404;
        private const int ERROR_NOT_CONNECTED = 2250;
        private const int ERROR_OPEN_FILES = 2401;
        private const int ERROR_LOGON_FAILURE = 1326;

        // Created with excel formula:
        // ="new ErrorClass("&A1&", """&PROPER(SUBSTITUTE(MID(A1,7,LEN(A1)-6), "_", " "))&"""), "
        private static readonly ErrorClass[] Error_list = new ErrorClass[]
        {
            new ErrorClass(ERROR_ACCESS_DENIED, "Error: Access Denied"),
            new ErrorClass(ERROR_ALREADY_ASSIGNED, "Error: Already Assigned"),
            new ErrorClass(ERROR_BAD_DEVICE, "Error: Bad Device"),
            new ErrorClass(ERROR_BAD_NET_NAME, "Error: Bad Net Name"),
            new ErrorClass(ERROR_BAD_PROVIDER, "Error: Bad Provider"),
            new ErrorClass(ERROR_CANCELLED, "Error: Cancelled"),
            new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
            new ErrorClass(ERROR_INVALID_ADDRESS, "Error: Invalid Address"),
            new ErrorClass(ERROR_INVALID_PARAMETER, "Error: Invalid Parameter"),
            new ErrorClass(ERROR_INVALID_PASSWORD, "Error: Invalid Password"),
            new ErrorClass(ERROR_INVALID_PASSWORDNAME, "Error: Invalid Password Format"),
            new ErrorClass(ERROR_MORE_DATA, "Error: More Data"),
            new ErrorClass(ERROR_NO_MORE_ITEMS, "Error: No More Items"),
            new ErrorClass(ERROR_NO_NET_OR_BAD_PATH, "Error: No Net Or Bad Path"),
            new ErrorClass(ERROR_NO_NETWORK, "Error: No Network"),
            new ErrorClass(ERROR_BAD_PROFILE, "Error: Bad Profile"),
            new ErrorClass(ERROR_CANNOT_OPEN_PROFILE, "Error: Cannot Open Profile"),
            new ErrorClass(ERROR_DEVICE_IN_USE, "Error: Device In Use"),
            new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
            new ErrorClass(ERROR_NOT_CONNECTED, "Error: Not Connected"),
            new ErrorClass(ERROR_OPEN_FILES, "Error: Open Files"),
            new ErrorClass(ERROR_LOGON_FAILURE, "The user name or password is incorrect"),
            new ErrorClass(ERROR_PATH_NOT_FOUND, "The network path not found")
        };

        private readonly string _networkName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNetworkFileShare"/> class.
        /// </summary>
        /// <param name="networkName">Address of the file share</param>
        /// <param name="credentials">Username and password for accessing the file share</param>
        public WindowsNetworkFileShare(string networkName, NetworkCredential credentials)
        {
            if (!Platform.IsWindows)
            {
                throw new PlatformNotSupportedException("WindowsNetworkFileShare only works on Windows");
            }

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
                : string.Format(@"{0}\{1}", credentials.Domain, credentials.UserName);

            var result = WNetUseConnection(IntPtr.Zero, netResource, credentials.Password, userName, 0, null, null, null);

            if (result != 0)
            {
                throw new Exception("Error connecting to remote share " + result + " " + GetErrorForNumber(result));
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WindowsNetworkFileShare"/> class.
        /// </summary>
        ~WindowsNetworkFileShare()
        {
            Dispose(false);
        }

        /// <summary>
        /// Scope of the file share
        /// </summary>
        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        }

        /// <summary>
        /// Type of network resource
        /// </summary>
        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        /// <summary>
        /// The display options for the network object in a network browsing user interface
        /// </summary>
        public enum ResourceDisplaytype : int
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
        /// Retrieves the most recent extended error code set by a WNet function
        /// <para/>P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora"/>
        /// </summary>
        /// <param name="error">The error code reported by the network provider.</param>
        /// <param name="errorBuf">String variable to receive the description of the error</param>
        /// <param name="errorBufSize">Size of error buffer</param>
        /// <param name="nameBuf">String variable to receive the network provider raising the error</param>
        /// <param name="nameBufSize">Size of name buffer</param>
        /// <returns>If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.<para/>If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.</returns>
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetGetLastError(
            out int error,
            out StringBuilder errorBuf,
            int errorBufSize,
            out StringBuilder nameBuf,
            int nameBufSize);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get a description for an error returned by a P/Invoke call
        /// </summary>
        /// <param name="errNum">Error code</param>
        /// <returns>An error message</returns>
        internal static string GetErrorForNumber(int errNum)
        {
            if (!Error_list.Any(e => e.Num == errNum))
            {
                return "Error: Unknown, " + errNum;
            }
            else
            {
                return Error_list.First(e => e.Num == errNum).Message;
            }
        }

        /// <summary>
        /// Disposes the object, cancels connection with file share
        /// </summary>
        /// <param name="disposing">Not used</param>
        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        /// <summary>
        /// Makes a connection to a network resource and can redirect a local device to the network resource.
        /// <para/>P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a"/>
        /// </summary>
        /// <param name="netResource">Network resource to interact with</param>
        /// <param name="password">Password for making the network connection</param>
        /// <param name="username">Username for making the network connection</param>
        /// <param name="flags">A set of connection options - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a#parameters"/></param>
        /// <returns>An integer representing the result - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetaddconnection2a#return-value"/></returns>
        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(
            NetResource netResource,
            string password,
            string username,
            int flags);

        /// <summary>
        /// Cancels an existing network connection, removes remembered network connections that are not currently connected.
        /// <para/>P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a"/>
        /// </summary>
        /// <param name="name">
        /// Pointer to a constant null-terminated string that specifies the name of either the redirected local device or the remote network resource to disconnect from.<para/>
        /// If this parameter specifies a redirected local device, the function cancels only the specified device redirection. If the parameter specifies a remote network resource, all connections without devices are canceled.
        /// </param>
        /// <param name="flags">Connection type - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a#parameters"/></param>
        /// <param name="force">Specifies whether the disconnection should occur if there are open files or jobs on the connection. If this parameter is FALSE, the function fails if there are open files or jobs.</param>
        /// <returns>An integer representing the result - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetcancelconnection2a#return-value"/></returns>
        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(
            string name,
            int flags,
            bool force);

        /// <summary>
        /// Makes a connection to a network resource. Can redirect a local device to a network resource.
        /// <para/>P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetuseconnectiona"/>
        /// </summary>
        /// <param name="hwndOwner">Handle to a window that the provider of network resources can use as an owner window for dialog boxes</param>
        /// <param name="netResource">Network resource to interact with</param>
        /// <param name="password">A null-terminated string that specifies a password to be used in making the network connection</param>
        /// <param name="username">A null-terminated string that specifies a user name for making the connection</param>
        /// <param name="flags">Set of bit flags describing the connection</param>
        /// <param name="lpAccessName">Pointer to a buffer that receives system requests on the connection</param>
        /// <param name="lpBufferSize">Pointer to a variable that specifies the size of the lpAccessName buffer, in characters.<para />If the call fails because the buffer is not large enough, the function returns the required buffer size in this location</param>
        /// <param name="lpResult">Pointer to a variable that receives additional information about the connection</param>
        /// <returns>An integer representing the result - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetuseconnectiona#return-value"/></returns>
        [DllImport("mpr.dll")]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            NetResource netResource,
            string password,
            string username,
            int flags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult);

        private struct ErrorClass
        {
            public int Num;
            public string Message;

            public ErrorClass(int num, string message)
            {
                Num = num;
                Message = message;
            }
        }

        /// <summary>
        /// The NETRESOURCE structure contains information about a network resource.
        /// More info on NetResource: <seealso href="https://msdn.microsoft.com/en-us/c53d078e-188a-4371-bdb9-fc023bc0c1ba"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
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
}

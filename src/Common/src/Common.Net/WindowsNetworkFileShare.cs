// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
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
        private readonly IMPR _mpr;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNetworkFileShare"/> class.
        /// </summary>
        /// <param name="networkName">Address of the file share</param>
        /// <param name="credentials">Username and password for accessing the file share</param>
        /// <param name="mpr">A class that handles calls to mpr.dll or performs same operations</param>
        public WindowsNetworkFileShare(string networkName, NetworkCredential credentials, IMPR mpr = null)
        {
            _mpr = mpr ?? new MPR();

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

            var result = _mpr.UseConnection(IntPtr.Zero, netResource, credentials.Password, userName, 0, null, null, null);

            if (result != 0)
            {
                throw new ExternalException("Error connecting to remote share - Code: " + result + ", " + GetErrorForNumber(result));
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
        public enum ResourceScope
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
        public enum ResourceType
        {
            Any = 0,
            Disk = 1,
            Print = 2,
#pragma warning disable S4016 // Enumeration members should not be named "Reserved"
            Reserved = 8,
#pragma warning restore S4016 // Enumeration members should not be named "Reserved"
        }

        /// <summary>
        /// The display options for the network object in a network browsing user interface
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
        /// Retrieves the most recent extended error code set by a WNet function
        /// <para/>Wraps an underlying P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora"/>
        /// </summary>
        /// <param name="error">The error code reported by the network provider.</param>
        /// <param name="errorBuf">String variable to receive the description of the error</param>
        /// <param name="errorBufSize">Size of error buffer</param>
        /// <param name="nameBuf">String variable to receive the network provider raising the error</param>
        /// <param name="nameBufSize">Size of name buffer</param>
        /// <returns>If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.<para/>If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.</returns>
        public int GetLastError(
            out int error,
            out StringBuilder errorBuf,
            int errorBufSize,
            out StringBuilder nameBuf,
            int nameBufSize)
        {
            return _mpr.GetLastError(out error, out errorBuf, errorBufSize, out nameBuf, nameBufSize);
        }

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
            _mpr.CancelConnection(_networkName, 0, true);
        }

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
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        [StructLayout(LayoutKind.Sequential)]
        internal class NetResource
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
